/* 
 * Copyright 2012-2014 Aerospike, Inc.
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
namespace Aerospike.Client
{
	/// <summary>
	/// Task used to poll for server task completion.
	/// </summary>
	public abstract class Task
	{
		protected internal readonly Cluster cluster;
		private bool done;

		/// <summary>
		/// Initialize task with fields needed to query server nodes.
		/// </summary>
		public Task(Cluster cluster, bool done)
		{
			this.cluster = cluster;
			this.done = done;
		}


		/// <summary>
		/// Wait for asynchronous task to complete using default sleep interval.
		/// </summary>
		public void Wait()
		{
			Wait(1000);
		}

		/// <summary>
		/// Wait for asynchronous task to complete using given sleep interval.
		/// </summary>
		public void Wait(int sleepInterval)
		{
			while (!done)
			{
				Util.Sleep(sleepInterval);
				done = IsDone();
			}
		}

		/// <summary>
		/// Query all nodes for task completion status.
		/// </summary>
		public abstract bool IsDone();
	}
}
