using System;
using System.Collections.Generic;
using System.Linq;

public interface ISignal
	{
		Delegate listener { get; set; }
		void RemoveAllListeners();
	}



	public class 
		Signal : BaseSignal, ISignal
	{
		public event Action Listener = null;
		public event Action OnceListener = null;

		public void AddListener(Action callback)
		{
			Listener = this.AddUnique(Listener, callback);
		}

		public void AddOnce(Action callback)
		{
			OnceListener = this.AddUnique(OnceListener, callback);
		}

		public void RemoveListener(Action callback)
		{
			if (Listener != null)
				Listener -= callback;
		}
		public override List<Type> GetTypes()
		{
			return new List<Type>();
		}
		public void Dispatch()
		{
			if (Listener != null)
				Listener();
			if (OnceListener != null)
				OnceListener();
			OnceListener = null;
			base.Dispatch(null);
		}

		private Action AddUnique(Action listeners, Action callback)
		{
			if (listeners == null || !listeners.GetInvocationList().Contains(callback))
			{
				listeners += callback;
			}
			return listeners;
		}

		public override void RemoveAllListeners()
		{
			Listener = null;
			OnceListener = null;
			base.RemoveAllListeners();
		}

		public Delegate listener
		{
				get { return Listener ?? (Listener = delegate { }); }
				set { Listener = (Action) value; }
		}
	}

	

	/// Base concrete form for a Signal with one parameter
	public class Signal<T> : BaseSignal, ISignal
	{
		public event Action<T> Listener = null;
		public event Action<T> OnceListener = null;

		public void AddListener(Action<T> callback)
		{
			Listener = this.AddUnique(Listener, callback);
		}

		public void AddOnce(Action<T> callback)
		{
			OnceListener = this.AddUnique(OnceListener, callback);
		}

		public void RemoveListener(Action<T> callback)
		{
			if (Listener != null)
				Listener -= callback;
		}
		public override List<Type> GetTypes()
		{
			List<Type> retv = new List<Type>();
			retv.Add(typeof(T));
			return retv;
		}
		public void Dispatch(T type1)
		{
			if (Listener != null)
				Listener(type1);
			if (OnceListener != null)
				OnceListener(type1);
			OnceListener = null;
			object[] outv = { type1 };
			base.Dispatch(outv);
		}

		private Action<T> AddUnique(Action<T> listeners, Action<T> callback)
		{
			if (listeners == null || !listeners.GetInvocationList().Contains(callback))
			{
				listeners += callback;
			}
			return listeners;
		}

		public override void RemoveAllListeners()
		{
			Listener = null;
			OnceListener = null;
			base.RemoveAllListeners();
		}
		public Delegate listener
		{
			get { return Listener ?? (Listener = delegate { }); }
			set { Listener = (Action<T>)value; }
		}

		public void AddListener(object navBarButtonClick)
		{
			throw new NotImplementedException();
		}
	}
	
	/// Base concrete form for a Signal with two parameters
	public class Signal<T, U> : BaseSignal, ISignal
	{
		public event Action<T, U> Listener = null;
		public event Action<T, U> OnceListener = null;

		public void AddListener(Action<T, U> callback)
		{
			Listener = this.AddUnique(Listener, callback);
		}

		public void AddOnce(Action<T, U> callback)
		{
			OnceListener = this.AddUnique(OnceListener, callback);
		}

		public void RemoveListener(Action<T, U> callback)
		{
			if (Listener != null)
				Listener -= callback;
		}
		public override List<Type> GetTypes()
		{
			List<Type> retv = new List<Type>();
			retv.Add(typeof(T));
			retv.Add(typeof(U));
			return retv;
		}
		public void Dispatch(T type1, U type2)
		{
			if (Listener != null)
				Listener(type1, type2);
			if (OnceListener != null)
				OnceListener(type1, type2);
			OnceListener = null;
			object[] outv = { type1, type2 };
			base.Dispatch(outv);
		}
		private Action<T, U> AddUnique(Action<T, U> listeners, Action<T, U> callback)
		{
			if (listeners == null || !listeners.GetInvocationList().Contains(callback))
			{
				listeners += callback;
			}
			return listeners;
		}

		public override void RemoveAllListeners()
		{
			Listener = null;
			OnceListener = null;
			base.RemoveAllListeners();
		}
		public Delegate listener
		{
			get { return Listener ?? (Listener = delegate { }); }
			set { Listener = (Action<T,U>)value; }
		}
	}

	public class Signal<T, U, V> : BaseSignal, ISignal
	{
		public event Action<T, U, V> Listener = null;
		public event Action<T, U, V> OnceListener = null;

		public void AddListener(Action<T, U, V> callback)
		{
			Listener = this.AddUnique(Listener, callback);
		}

		public void AddOnce(Action<T, U, V> callback)
		{
			OnceListener = this.AddUnique(OnceListener, callback);
		}

		public void RemoveListener(Action<T, U, V> callback)
		{
			if (Listener != null)
				Listener -= callback;
		}
		public override List<Type> GetTypes()
		{
			List<Type> retv = new List<Type>();
			retv.Add(typeof(T));
			retv.Add(typeof(U));
			retv.Add(typeof(V));
			return retv;
		}
		public void Dispatch(T type1, U type2, V type3)
		{
			if (Listener != null)
				Listener(type1, type2, type3);
			if (OnceListener != null)
				OnceListener(type1, type2, type3);
			OnceListener = null;
			object[] outv = { type1, type2, type3 };
			base.Dispatch(outv);
		}
		private Action<T, U, V> AddUnique(Action<T, U, V> listeners, Action<T, U, V> callback)
		{
			if (listeners == null || !listeners.GetInvocationList().Contains(callback))
			{
				listeners += callback;
			}
			return listeners;
		}
		public override void RemoveAllListeners()
		{
			Listener = null;
			OnceListener = null;
			base.RemoveAllListeners();
		}
		public Delegate listener
		{
			get { return Listener ?? (Listener = delegate { }); }
			set { Listener = (Action<T,U,V>)value; }
		}
	}

	/// Base concrete form for a Signal with four parameters
	public class Signal<T, U, V, W> : BaseSignal, ISignal
	{
		public event Action<T, U, V, W> Listener = null;
		public event Action<T, U, V, W> OnceListener = null;

		public void AddListener(Action<T, U, V, W> callback)
		{
			Listener = this.AddUnique(Listener, callback);
		}

		public void AddOnce(Action<T, U, V, W> callback)
		{
			OnceListener = this.AddUnique(OnceListener, callback);
		}

		public void RemoveListener(Action<T, U, V, W> callback)
		{
			if (Listener != null)
				Listener -= callback;
		}
		public override List<Type> GetTypes()
		{
			List<Type> retv = new List<Type>();
			retv.Add(typeof(T));
			retv.Add(typeof(U));
			retv.Add(typeof(V));
			retv.Add(typeof(W));
			return retv;
		}
		public void Dispatch(T type1, U type2, V type3, W type4)
		{
			if (Listener != null)
				Listener(type1, type2, type3, type4);
			if (OnceListener != null)
				OnceListener(type1, type2, type3, type4);
			OnceListener = null;
			object[] outv = { type1, type2, type3, type4 };
			base.Dispatch(outv);
		}

		private Action<T, U, V, W> AddUnique(Action<T, U, V, W> listeners, Action<T, U, V, W> callback)
		{
			if (listeners == null || !listeners.GetInvocationList().Contains(callback))
			{
				listeners += callback;
			}
			return listeners;
		}
		public override void RemoveAllListeners()
		{
			Listener = null;
			OnceListener = null;
			base.RemoveAllListeners();
		}
		public Delegate listener
		{
			get { return Listener ?? (Listener = delegate { }); }
			set { Listener = (Action<T,U,V,W>)value; }
		}
	}

/// Base concrete form for a Signal with five parameters
public class Signal<T, U, V, W, Z> : BaseSignal, ISignal
{
	public event Action<T, U, V, W,Z> Listener = null;
	public event Action<T, U, V, W,Z> OnceListener = null;

	public void AddListener(Action<T, U, V, W,Z> callback)
	{
		Listener = this.AddUnique(Listener, callback);
	}

	public void AddOnce(Action<T, U, V, W,Z> callback)
	{
		OnceListener = this.AddUnique(OnceListener, callback);
	}

	public void RemoveListener(Action<T, U, V, W,Z> callback)
	{
		if (Listener != null)
			Listener -= callback;
	}
	public override List<Type> GetTypes()
	{
		List<Type> retv = new List<Type>();
		retv.Add(typeof(T));
		retv.Add(typeof(U));
		retv.Add(typeof(V));
		retv.Add(typeof(W));
		retv.Add(typeof(Z));
		return retv;
	}
	public void Dispatch(T type1, U type2, V type3, W type4,Z type5)
	{
		if (Listener != null)
			Listener(type1, type2, type3, type4,type5);
		if (OnceListener != null)
			OnceListener(type1, type2, type3, type4,type5);
		OnceListener = null;
		object[] outv = { type1, type2, type3, type4 ,type5};
		base.Dispatch(outv);
	}

	private Action<T, U, V, W,Z> AddUnique(Action<T, U, V, W,Z> listeners, Action<T, U, V, W,Z> callback)
	{
		if (listeners == null || !listeners.GetInvocationList().Contains(callback))
		{
			listeners += callback;
		}
		return listeners;
	}
	public override void RemoveAllListeners()
	{
		Listener = null;
		OnceListener = null;
		base.RemoveAllListeners();
	}
	public Delegate listener
	{
		get { return Listener ?? (Listener = delegate { }); }
		set { Listener = (Action<T,U,V,W,Z>)value; }
	}
}

