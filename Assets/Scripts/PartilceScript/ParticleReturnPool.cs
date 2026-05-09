using UnityEngine;

public class ParticleReturnPool : MonoBehaviour,IPoolObject
{
    private ObjectPool _pool;

    public void InitPoolObject(ObjectPool pool)
    {
        _pool = pool;
    }

    public void ReturnToPool()
    {
        _pool.Return(gameObject);
    }

    public void OnParticleSystemStopped()
    {
        ReturnToPool();
    }

}
