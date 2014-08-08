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
using System;
using System.Collections.Concurrent;
using System.Threading;
using LuaInterface;

namespace Aerospike.Client
{
	public abstract class LuaStream
	{
		public static void LoadLibrary(Lua lua)
		{
			Type type = typeof(LuaStream);
			lua.RegisterFunction("stream.read", null, type.GetMethod("read", new Type[] { type }));
			lua.RegisterFunction("stream.write", null, type.GetMethod("write", new Type[] { type, typeof(object) }));
			lua.RegisterFunction("stream.readable", null, type.GetMethod("readable", new Type[] { type }));
			lua.RegisterFunction("stream.writeable", null, type.GetMethod("writeable", new Type[] { type }));
		}

		public static object read(LuaStream stream)
		{
			return stream.Read();
		}

		public static void write(LuaStream stream, object obj)
		{
			stream.Write(obj);
		}

		public static bool readable(LuaStream stream)
		{
			return stream.Readable();
		}

		public static bool writeable(LuaStream stream)
		{
			return stream.Writeable();
		}

		public abstract object Read();
		public abstract void Write(object obj);
		public abstract bool Readable();
		public abstract bool Writeable();
	}

	public sealed class LuaInputStream : LuaStream
	{
		private readonly BlockingCollection<object> queue;

		public LuaInputStream(BlockingCollection<object> queue)
		{
			this.queue = queue;
		}

		public override object Read()
		{
			try
			{
				return queue.Take();
			}
			catch (ThreadInterruptedException)
			{
				return null;
			}
		}

		public override void Write(object value)
		{
			throw new Exception("LuaInputStream is not writeable.");
		}

		public override bool Readable()
		{
			return true;
		}

		public override bool Writeable()
		{
			return false;
		}

		public override string ToString()
		{
			return typeof(LuaInputStream).FullName;
		}
	}

	public sealed class LuaOutputStream : LuaStream
	{
		private readonly ResultSet resultSet;

		public LuaOutputStream(ResultSet resultSet)
		{
			this.resultSet = resultSet;
		}

		public override object Read()
		{
			throw new Exception("LuaOutputStream is not readable.");
		}

		public override void Write(object obj)
		{
			object target = LuaInstance.LuaToObject(obj);
			resultSet.Put(target);
		}

		public override bool Readable()
		{
			return false;
		}

		public override bool Writeable()
		{
			return true;
		}

		public override string ToString()
		{
			return typeof(LuaOutputStream).FullName;
		}
	}
}
