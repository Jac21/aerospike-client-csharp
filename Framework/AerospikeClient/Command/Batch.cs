/* 
 * Copyright 2012-2019 Aerospike, Inc.
 *
 * Portions may be licensed to Aerospike, Inc. under one or more contributor
 * license agreements.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */
using System;
using System.Collections.Generic;
using System.Threading;

namespace Aerospike.Client
{
	//-------------------------------------------------------
	// ReadList
	//-------------------------------------------------------

	public sealed class BatchReadListCommand : MultiCommand
	{
		private readonly Executor parent;
		private readonly BatchNode batch;
		private readonly BatchPolicy policy;
		private readonly List<BatchRead> records;

		public BatchReadListCommand(Executor parent, BatchNode batch, BatchPolicy policy, List<BatchRead> records)
			: base(false)
		{
			this.parent = parent;
			this.batch = batch;
			this.policy = policy;
			this.records = records;
		}

		protected internal override void WriteBuffer()
		{
			SetBatchRead(policy, records, batch);
		}

		protected internal override void ParseRow(Key key)
		{
			BatchRead record = records[batchIndex];

			if (Util.ByteArrayEquals(key.digest, record.key.digest))
			{
				if (resultCode == 0)
				{
					record.record = ParseRecord();
				}
			}
			else
			{
				throw new AerospikeException.Parse("Unexpected batch key returned: " + key.ns + ',' + ByteUtil.BytesToHexString(key.digest) + ',' + batchIndex);
			}
		}

		protected internal override bool ShouldRetryBatch()
		{
			return (policy.replica == Replica.SEQUENCE || policy.replica == Replica.PREFER_RACK) && (parent == null || ! parent.IsDone());
		}

		protected internal override bool RetryBatch(Cluster cluster, int socketTimeout, int totalTimeout, DateTime deadline, int iteration, int commandSentCounter)
		{
			// Retry requires keys for this node to be split among other nodes.
			// This is both recursive and exponential.
			List<BatchNode> batchNodes = BatchNode.GenerateList(cluster, policy, records, sequence, batch);

			if (batchNodes.Count == 1 && batchNodes[0].node == batch.node)
			{
				// Batch node is the same.  Go through normal retry.
				return false;
			}

			// Run batch requests sequentially in same thread.
			foreach (BatchNode batchNode in batchNodes)
			{
				MultiCommand command = new BatchReadListCommand(parent, batchNode, policy, records);
				command.sequence = sequence;
				command.Execute(cluster, policy, null, batchNode.node, true, socketTimeout, totalTimeout, deadline, iteration, commandSentCounter);
			}
			return true;
		}
	}

	//-------------------------------------------------------
	// GetArray
	//-------------------------------------------------------
	
	public sealed class BatchGetArrayCommand : MultiCommand
	{
		private readonly Executor parent;
		private readonly BatchNode batch;
		private readonly BatchPolicy policy;
		private readonly Key[] keys;
		private readonly string[] binNames;
		private readonly Record[] records;
		private readonly int readAttr;

		public BatchGetArrayCommand
		(
			Executor parent,
			BatchNode batch,
			BatchPolicy policy,
			Key[] keys,
			string[] binNames,
			Record[] records,
			int readAttr
		) : base(false)
		{
			this.parent = parent;
			this.batch = batch;
			this.policy = policy;
			this.keys = keys;
			this.binNames = binNames;
			this.records = records;
			this.readAttr = readAttr;
		}

		protected internal override void WriteBuffer()
		{
			SetBatchRead(policy, keys, batch, binNames, readAttr);
		}

		protected internal override void ParseRow(Key key)
		{
			if (Util.ByteArrayEquals(key.digest, keys[batchIndex].digest))
			{
				if (resultCode == 0)
				{
					records[batchIndex] = ParseRecord();
				}
			}
			else
			{
				throw new AerospikeException.Parse("Unexpected batch key returned: " + key.ns + ',' + ByteUtil.BytesToHexString(key.digest) + ',' + batchIndex);
			}
		}

		protected internal override bool ShouldRetryBatch()
		{
			return (policy.replica == Replica.SEQUENCE || policy.replica == Replica.PREFER_RACK) && (parent == null || !parent.IsDone());
		}

		protected internal override bool RetryBatch(Cluster cluster, int socketTimeout, int totalTimeout, DateTime deadline, int iteration, int commandSentCounter)
		{
			// Retry requires keys for this node to be split among other nodes.
			// This is both recursive and exponential.
			List<BatchNode> batchNodes = BatchNode.GenerateList(cluster, policy, keys, sequence, batch);

			if (batchNodes.Count == 1 && batchNodes[0].node == batch.node)
			{
				// Batch node is the same.  Go through normal retry.
				return false;
			}

			// Run batch requests sequentially in same thread.
			foreach (BatchNode batchNode in batchNodes)
			{
				MultiCommand command = new BatchGetArrayCommand(parent, batchNode, policy, keys, binNames, records, readAttr);
				command.sequence = sequence;
				command.Execute(cluster, policy, null, batchNode.node, true, socketTimeout, totalTimeout, deadline, iteration, commandSentCounter);
			}
			return true;
		}
	}

	//-------------------------------------------------------
	// ExistsArray
	//-------------------------------------------------------
	
	public sealed class BatchExistsArrayCommand : MultiCommand
	{
		private readonly Executor parent;
		private readonly BatchNode batch;
		private readonly BatchPolicy policy;
		private readonly Key[] keys;
		private readonly bool[] existsArray;

		public BatchExistsArrayCommand
		(
			Executor parent,
			BatchNode batch,
			BatchPolicy policy,
			Key[] keys,
			bool[] existsArray
		) : base(false)
		{
			this.parent = parent;
			this.batch = batch;
			this.policy = policy;
			this.keys = keys;
			this.existsArray = existsArray;
		}

		protected internal override void WriteBuffer()
		{
			SetBatchRead(policy, keys, batch, null, Command.INFO1_READ | Command.INFO1_NOBINDATA);
		}

		protected internal override void ParseRow(Key key)
		{
			if (opCount > 0)
			{
				throw new AerospikeException.Parse("Received bins that were not requested!");
			}

			if (Util.ByteArrayEquals(key.digest, keys[batchIndex].digest))
			{
				existsArray[batchIndex] = resultCode == 0;
			}
			else
			{
				throw new AerospikeException.Parse("Unexpected batch key returned: " + key.ns + ',' + ByteUtil.BytesToHexString(key.digest) + ',' + batchIndex);
			}
		}

		protected internal override bool ShouldRetryBatch()
		{
			return (policy.replica == Replica.SEQUENCE || policy.replica == Replica.PREFER_RACK) && (parent == null || !parent.IsDone());
		}

		protected internal override bool RetryBatch(Cluster cluster, int socketTimeout, int totalTimeout, DateTime deadline, int iteration, int commandSentCounter)
		{
			// Retry requires keys for this node to be split among other nodes.
			// This is both recursive and exponential.
			List<BatchNode> batchNodes = BatchNode.GenerateList(cluster, policy, keys, sequence, batch);

			if (batchNodes.Count == 1 && batchNodes[0].node == batch.node)
			{
				// Batch node is the same.  Go through normal retry.
				return false;
			}

			// Run batch requests sequentially in same thread.
			foreach (BatchNode batchNode in batchNodes)
			{
				MultiCommand command = new BatchExistsArrayCommand(parent, batchNode, policy, keys, existsArray);
				command.sequence = sequence;
				command.Execute(cluster, policy, null, batchNode.node, true, socketTimeout, totalTimeout, deadline, iteration, commandSentCounter);
			}
			return true;
		}
	}
}
