using System.Collections;
using UnityEngine;

public class CoroutineWithData
{
    public Coroutine coroutine { get; private set; }
    public object result;
    private IEnumerator target;
    private MonoBehaviour owner;

    public CoroutineWithData(MonoBehaviour owner, IEnumerator target)
    {
        this.target = target;
        this.owner = owner;
        this.coroutine = this.owner.StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        while (target.MoveNext())
        {
            result = target.Current;
            yield return result;
        }
    }

    public void Stop()
    {
        this.owner.StopCoroutine(Run());
        this.result = null;
    }
}
