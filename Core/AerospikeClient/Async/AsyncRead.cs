/* 
 * Copyright 2012-2018 Aerospike, Inc.
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
using System.Collections.Generic;

namespace Aerospike.Client
{
	public class AsyncRead : AsyncSingleCommand
	{
		private readonly RecordListener listener;
		protected internal readonly Key key;
		private readonly string[] binNames;
		protected Record record;

		public AsyncRead(AsyncCluster cluster, Policy policy, RecordListener listener, Key key, string[] binNames, bool isRead) 
			: base(cluster, policy, new Partition(key), isRead)
		{
			this.listener = listener;
			this.key = key;
			this.binNames = binNames;
		}

		public AsyncRead(AsyncRead other)
			: base(other)
		{
			this.listener = other.listener;
			this.key = other.key;
			this.binNames = other.binNames;
		}

		protected internal override AsyncCommand CloneCommand()
		{
			return new AsyncRead(this);
		}

		protected internal override void WriteBuffer()
		{
			SetRead(policy, key, binNames);
		}

		protected internal sealed override void ParseResult()
		{
			int resultCode = dataBuffer[dataOffset + 5];
			int generation = ByteUtil.BytesToInt(dataBuffer, dataOffset + 6);
			int expiration = ByteUtil.BytesToInt(dataBuffer, dataOffset + 10);
			int fieldCount = ByteUtil.BytesToShort(dataBuffer, dataOffset + 18);
			int opCount = ByteUtil.BytesToShort(dataBuffer, dataOffset + 20);
			dataOffset += Command.MSG_REMAINING_HEADER_SIZE;

			if (resultCode == 0)
			{
				if (opCount == 0)
				{
					// Bin data was not returned.
					record = new Record(null, generation, expiration);
				}
				else
				{
					record = ParseRecord(opCount, fieldCount, generation, expiration);
				}
			}
			else
			{
				if (resultCode == ResultCode.KEY_NOT_FOUND_ERROR)
				{
					HandleNotFound(resultCode);
				}
				else
				{
					throw new AerospikeException(resultCode);
				}
			}
		}

		protected internal virtual void HandleNotFound(int resultCode)
		{
			// Do nothing in default case. Record will be null.
		}

		private Record ParseRecord(int opCount, int fieldCount, int generation, int expiration)
		{
			// There can be fields in the response (setname etc).
			// But for now, ignore them. Expose them to the API if needed in the future.
			if (fieldCount > 0)
			{
				// Just skip over all the fields
				for (int i = 0; i < fieldCount; i++)
				{
					int fieldSize = ByteUtil.BytesToInt(dataBuffer, dataOffset);
					dataOffset += 4 + fieldSize;
				}
			}

			Dictionary<string, object> bins = null;

			for (int i = 0; i < opCount; i++)
			{
				int opSize = ByteUtil.BytesToInt(dataBuffer, dataOffset);
				byte particleType = dataBuffer[dataOffset + 5];
				byte nameSize = dataBuffer[dataOffset + 7];
				string name = ByteUtil.Utf8ToString(dataBuffer, dataOffset + 8, nameSize);
				dataOffset += 4 + 4 + nameSize;

				int particleBytesSize = (int)(opSize - (4 + nameSize));
				object value = ByteUtil.BytesToParticle(particleType, dataBuffer, dataOffset, particleBytesSize);
				dataOffset += particleBytesSize;

				if (bins == null)
				{
					bins = new Dictionary<string, object>();
				}
				AddBin(bins, name, value);
			}
			return new Record(bins, generation, expiration);
		}

		protected internal virtual void AddBin(Dictionary<string, object> bins, string name, object value)
		{
			bins[name] = value;
		}

		protected internal override void OnSuccess()
		{
			if (listener != null)
			{
				listener.OnSuccess(key, record);
			}
		}

		protected internal override void OnFailure(AerospikeException e)
		{
			if (listener != null)
			{
				listener.OnFailure(e);
			}
		}
	}
}
