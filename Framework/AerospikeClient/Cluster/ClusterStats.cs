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
using System.Threading;

namespace Aerospike.Client
{
	/// <summary>
	/// Cluster statistics.
	/// </summary>
	public sealed class ClusterStats
	{
		/// <summary>
		/// Statistics for each node.
		/// </summary>
		public readonly NodeStats[] nodes;

		/// <summary>
		/// Number of active threads executing sync batch/scan/query commands.
		/// </summary>
		public readonly int threadsInUse;

		/// <summary>
		/// Number of active async completion ports.
		/// </summary>
		public readonly int completionPortsInUse;

		/// <summary>
		/// Cluster statistics constructor.
		/// </summary>
		public ClusterStats(NodeStats[] nodes)
		{
			this.nodes = nodes;

#if NETFRAMEWORK
			int workerThreadsMax;
			int completionPortThreadsMax;
			ThreadPool.GetMaxThreads(out workerThreadsMax, out completionPortThreadsMax);

			int workerThreads;
			int completionPortThreads;
			ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);

			this.threadsInUse = workerThreadsMax - workerThreads;
			this.completionPortsInUse = completionPortThreadsMax - completionPortThreads;
#else
            // .NET CORE does not define ThreadPool.GetAvailableThreads().
            this.threadsInUse = 0;
            this.completionPortsInUse = 0;
#endif
        }
    }

	/// <summary>
	/// Node statistics.
	/// </summary>
	public sealed class NodeStats
	{
		/// <summary>
		/// Node for which statistics are generated.
		/// </summary>
		public readonly Node node;

		/// <summary>
		/// Connection statistics for sync commands.
		/// </summary>
		public readonly ConnectionStats syncStats;

		/// <summary>
		/// Connection statistics for async commands.
		/// </summary>
		public readonly ConnectionStats asyncStats;

		/// <summary>
		/// Node statistics constructor.
		/// </summary>
		public NodeStats(Node node)
		{
			this.node = node;
			this.syncStats = node.GetConnectionStats();

			if (node is AsyncNode)
			{
				this.asyncStats = ((AsyncNode)node).GetAsyncConnectionStats();
			}
			else
			{
				this.asyncStats = new ConnectionStats(0, 0);
			}
		}
	}

	/// <summary>
	/// Connection statistics.
	/// </summary>
	public sealed class ConnectionStats
	{
		/// <summary>
		/// Connections residing in connection pool(s).
		/// </summary>
		public readonly int inPool;

		/// <summary>
		/// Active connections in currently executing commands.
		/// </summary>
		public readonly int inUse;

		/// <summary>
		/// Connection statistics constructor.
		/// </summary>
		public ConnectionStats(int inPool, int inUse)
		{
			this.inPool = inPool;
			this.inUse = inUse;
		}
	}
}
