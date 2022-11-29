﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BBG
{
	public class ObjectPool
	{
		#region Member Variables

		private GameObject			objectPrefab		= null;
		private List<PoolObject>	poolObjects = new List<PoolObject>();
		private Transform			parent				= null;
		private PoolBehaviour		poolBehaviour		= PoolBehaviour.GameObject;

		#endregion

		#region Enums

		public enum PoolBehaviour
		{
			GameObject,
			CanvasGroup
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Initializes a new instance of the ObjectPooler class.
		/// </summary>
		public ObjectPool(GameObject objectPrefab, int initialSize, Transform parent = null, PoolBehaviour poolBehaviour = PoolBehaviour.GameObject)
		{
			this.objectPrefab	= objectPrefab;
			this.parent			= parent;
			this.poolBehaviour	= poolBehaviour;

			for (int i = 0; i < initialSize; i++)
			{
				CreateObject();
			}
		}

		public static Transform CreatePoolContainer(Transform containerParent, string name = "pool_container")
		{
			GameObject container = new GameObject(name);

			container.SetActive(false);
			container.transform.SetParent(containerParent);

			return container.transform;
		}

		public static void ReturnObjectToPool(GameObject gameObject)
		{
			PoolObject poolObject = gameObject.GetComponent<PoolObject>();

			if (poolObject == null)
			{
				Debug.LogWarning("[ObjectPool] ReturnToPool: The gameObject " + gameObject.name + " does not belong to any pool.");
				return;
			}

			poolObject.pool.ReturnObjectToPool(poolObject);
		}

		/// <summary>
		/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
		/// Objects are returned to the pool by setting their active state to false.
		/// </summary>
		public GameObject GetObject()
		{
			bool temp;

			return GetObject(out temp);
		}

		/// <summary>
		/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
		/// Objects are returned to the pool by setting their active state to false.
		/// </summary>
		public GameObject GetObject(out bool instantiated)
		{
			instantiated = false;

			PoolObject poolObject = null;

			for (int i = 0; i < poolObjects.Count; i++)
			{
				if (poolObjects[i].isInPool)
				{
					poolObject = poolObjects[i];

					break;
				}
			}

			if (poolObject == null)
			{
				poolObject		= CreateObject();
				instantiated	= true;
			}

			switch (poolBehaviour)
			{
				case PoolBehaviour.GameObject:
					poolObject.gameObject.SetActive(true);
					break;
				case PoolBehaviour.CanvasGroup:
					poolObject.canvasGroup.alpha			= 1f;
					poolObject.canvasGroup.interactable		= true;
					poolObject.canvasGroup.blocksRaycasts	= true;
					break;
			}

			poolObject.isInPool = false;

			return poolObject.gameObject;
		}

		/// <summary>
		/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
		/// Objects are returned to the pool by setting their active state to false.
		/// </summary>
		public GameObject GetObject(Transform parent)
		{
			bool temp;

			return GetObject(parent, out temp);
		}

		/// <summary>
		/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
		/// Objects are returned to the pool by setting their active state to false.
		/// </summary>
		public GameObject GetObject(Transform parent, out bool instantiated)
		{
			GameObject obj = GetObject(out instantiated);

			obj.transform.SetParent(parent, false);

			return obj;
		}

		/// <summary>
		/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
		/// Objects are returned to the pool by setting their active state to false.
		/// </summary>
		public T GetObject<T>(Transform parent) where T : Component
		{
			return GetObject(parent).GetComponent<T>();
		}

		/// <summary>
		/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
		/// Objects are returned to the pool by setting their active state to false.
		/// </summary>
		public T GetObject<T>(Transform parent, out bool instantiated) where T : Component
		{
			return GetObject(parent, out instantiated).GetComponent<T>();
		}

		/// <summary>
		/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
		/// Objects are returned to the pool by setting their active state to false.
		/// </summary>
		public T GetObject<T>() where T : Component
		{
			return GetObject().GetComponent<T>();
		}

		/// <summary>
		/// Returns an object, if there is no object that can be returned from instantiatedObjects then it creates a new one.
		/// Objects are returned to the pool by setting their active state to false.
		/// </summary>
		public T GetObject<T>(out bool instantiated) where T : Component
		{
			return GetObject(out instantiated).GetComponent<T>();
		}

		/// <summary>
		/// Sets all instantiated GameObjects to de-active
		/// </summary>
		public void ReturnAllObjectsToPool()
		{
			for (int i = 0; i < poolObjects.Count; i++)
			{
				ReturnObjectToPool(poolObjects[i]);
			}
		}

		/// <summary>
		/// Returns the object to pool.
		/// </summary>
		public void ReturnObjectToPool(PoolObject poolObject)
		{
			poolObject.transform.SetParent(parent, false);

			switch (poolBehaviour)
			{
				case PoolBehaviour.GameObject:
					poolObject.gameObject.SetActive(false);
					break;
				case PoolBehaviour.CanvasGroup:
					poolObject.canvasGroup.alpha			= 0f;
					poolObject.canvasGroup.interactable		= false;
					poolObject.canvasGroup.blocksRaycasts	= false;
					break;
			}

			poolObject.isInPool = true;
		}

		/// <summary>
		/// Destroies all objects.
		/// </summary>
		public void DestroyAllObjects()
		{
			for (int i = 0; i < poolObjects.Count; i++)
			{
				GameObject.Destroy(poolObjects[i]);
			}

			poolObjects.Clear();
		}

		#endregion

		#region Private Methods

		private PoolObject CreateObject()
		{
			GameObject obj = GameObject.Instantiate(objectPrefab);

			PoolObject poolObject = obj.AddComponent<PoolObject>();

			poolObject.pool = this;

			if (poolBehaviour == PoolBehaviour.CanvasGroup)
			{
				poolObject.canvasGroup = obj.GetComponent<CanvasGroup>();

				if (poolObject.canvasGroup == null)
				{
					poolObject.canvasGroup = obj.AddComponent<CanvasGroup>();
				}
			}

			poolObjects.Add(poolObject);

			ReturnObjectToPool(poolObject);

			return poolObject;
		}

		#endregion
	}
}
