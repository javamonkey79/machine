﻿using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.FiniteState
{
	public class FstResult<TData, TOffset>
	{
		private readonly NullableValue<TOffset>[,] _registers;
		private readonly TData _output;
		private readonly VariableBindings _varBindings;
		private readonly string _id;
		private readonly int _priority;
		private readonly bool _isLazy;
		private readonly Annotation<TOffset> _nextAnn;
		private readonly int _depth;

		internal FstResult(string id, NullableValue<TOffset>[,] registers, TData output, VariableBindings varBindings, int priority, bool isLazy, Annotation<TOffset> nextAnn, int depth)
		{
			_id = id;
			_registers = registers;
			_output = output;
			_varBindings = varBindings;
			_priority = priority;
			_isLazy = isLazy;
			_nextAnn = nextAnn;
			_depth = depth;
		}

		public string ID
		{
			get { return _id; }
		}

		public NullableValue<TOffset>[,] Registers
		{
			get { return _registers; }
		}

		public TData Output
		{
			get { return _output; }
		}

		public VariableBindings VariableBindings
		{
			get { return _varBindings; }
		}

		public int Priority
		{
			get { return _priority; }
		}

		public Annotation<TOffset> NextAnnotation
		{
			get { return _nextAnn; }
		}

		public bool IsLazy
		{
			get { return _isLazy; }
		}

		public int Depth
		{
			get { return _depth; }
		}
	}
}
