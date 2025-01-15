using System;
using System.Collections.Generic;
using System.Linq;

public class BaseSignal : IBaseSignal
	{
		public event Action<IBaseSignal, object[]> BaseListener = null;
		
		public event Action<IBaseSignal, object[]> OnceBaseListener = null;
		
		public void Dispatch(object[] args) 
		{ 
			if (BaseListener != null)
				BaseListener(this, args);
			if (OnceBaseListener != null)
				OnceBaseListener(this, args);
			OnceBaseListener = null;
		}

		public virtual List<Type> GetTypes() { return new List<Type>(); }
		
		public void AddListener(Action<IBaseSignal, object[]> callback)
		{
			BaseListener = AddUnique(BaseListener, callback);
		}
		
		public void AddOnce(Action<IBaseSignal, object[]> callback)
		{
			OnceBaseListener = AddUnique(OnceBaseListener, callback);
		}

		private Action<T, U> AddUnique<T,U>(Action<T, U> listeners, Action<T, U> callback)
		{
			if (listeners == null || !listeners.GetInvocationList().Contains(callback))
			{
				listeners += callback;
			}
			return listeners;
		}
		
		public void RemoveListener(Action<IBaseSignal, object[]> callback)
		{
			if (BaseListener != null)
				BaseListener -= callback;
		}
		
		public virtual void RemoveAllListeners()
		{
			BaseListener = null;
			OnceBaseListener = null;
		}
	   
	}


public interface IBaseSignal
{
	void Dispatch(object[] args);
		
	void AddListener(Action<IBaseSignal, object[]> callback);

	void AddOnce (Action<IBaseSignal, object[]> callback);
	void RemoveListener(Action<IBaseSignal, object[]> callback);

	void RemoveAllListeners();
	List<Type> GetTypes();
}
