using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBG
{
	public abstract class RecyclableListItem<T> : ClickableListItem
	{
		#region Abstract Methods

		public abstract void Initialize(T dataObject);
		public abstract void Setup(T dataObject);
		public abstract void Refresh(T dataObject);
		public abstract void Removed();

		#endregion
	}
}
