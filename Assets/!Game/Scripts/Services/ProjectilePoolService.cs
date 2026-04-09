using System;
using System.Collections.Generic;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Skills;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Game.Scripts.Services
{
    public class ProjectilePoolService : IService
    {
        private sealed class Pool
        {
            public readonly Queue<Projectile> InactiveProjectiles = new();
            public readonly Func<GameObject> Factory;
            public readonly Transform Root;

            public Pool(Func<GameObject> factory, Transform root)
            {
                Factory = factory;
                Root = root;
            }
        }

        private readonly Dictionary<GameObject, Pool> _prefabPools = new();
        private readonly Dictionary<string, Pool> _runtimePools = new();
        private readonly Transform _poolRoot;

        public ProjectilePoolService()
        {
            var poolRootObject = new GameObject("ProjectilePools");
            Object.DontDestroyOnLoad(poolRootObject);
            _poolRoot = poolRootObject.transform;
        }

        public Projectile GetProjectile(GameObject projectilePrefab, Vector3 position, Quaternion rotation)
        {
            if (projectilePrefab == null)
                return null;

            Pool pool = GetOrCreatePrefabPool(projectilePrefab);
            return AcquireProjectile(pool, position, rotation, projectilePrefab, null);
        }

        public Projectile GetRuntimeProjectile(string poolKey, Func<GameObject> factory, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrWhiteSpace(poolKey) || factory == null)
                return null;

            Pool pool = GetOrCreateRuntimePool(poolKey, factory);
            return AcquireProjectile(pool, position, rotation, null, poolKey);
        }

        public void Release(Projectile projectile, GameObject prefabKey, string runtimePoolKey)
        {
            if (projectile == null)
                return;

            Pool pool = null;
            if (prefabKey != null)
                _prefabPools.TryGetValue(prefabKey, out pool);
            else if (!string.IsNullOrWhiteSpace(runtimePoolKey))
                _runtimePools.TryGetValue(runtimePoolKey, out pool);

            if (pool == null)
            {
                Object.Destroy(projectile.gameObject);
                return;
            }

            Transform projectileTransform = projectile.transform;
            projectile.gameObject.SetActive(false);
            projectileTransform.SetParent(pool.Root, false);
            pool.InactiveProjectiles.Enqueue(projectile);
        }

        private Pool GetOrCreatePrefabPool(GameObject projectilePrefab)
        {
            if (_prefabPools.TryGetValue(projectilePrefab, out Pool pool))
                return pool;

            var root = CreatePoolRoot($"Prefab_{projectilePrefab.name}");
            pool = new Pool(() => Object.Instantiate(projectilePrefab), root);
            _prefabPools.Add(projectilePrefab, pool);
            return pool;
        }

        private Pool GetOrCreateRuntimePool(string poolKey, Func<GameObject> factory)
        {
            if (_runtimePools.TryGetValue(poolKey, out Pool pool))
                return pool;

            var root = CreatePoolRoot($"Runtime_{poolKey}");
            pool = new Pool(factory, root);
            _runtimePools.Add(poolKey, pool);
            return pool;
        }

        private Projectile AcquireProjectile(
            Pool pool,
            Vector3 position,
            Quaternion rotation,
            GameObject prefabKey,
            string runtimePoolKey)
        {
            Projectile projectile = null;

            while (pool.InactiveProjectiles.Count > 0 && projectile == null)
                projectile = pool.InactiveProjectiles.Dequeue();

            if (projectile == null)
            {
                GameObject projectileObject = pool.Factory.Invoke();
                if (projectileObject == null)
                    return null;

                projectile = projectileObject.GetComponent<Projectile>() ?? projectileObject.AddComponent<Projectile>();
            }

            Transform projectileTransform = projectile.transform;
            projectileTransform.SetParent(null, false);
            projectileTransform.SetPositionAndRotation(position, rotation);
            projectile.gameObject.SetActive(true);
            projectile.BindPool(this, prefabKey, runtimePoolKey);
            return projectile;
        }

        private Transform CreatePoolRoot(string name)
        {
            var poolRootObject = new GameObject(name);
            poolRootObject.transform.SetParent(_poolRoot, false);
            return poolRootObject.transform;
        }
    }
}