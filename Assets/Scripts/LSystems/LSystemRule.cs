using System;
using UnityEngine;

[Serializable]
struct LSystemRule
{
    public string _input;
    public string _output;
    private LSystemRule((string left, string right) pair)
    {
        _input = pair.left;
        _output = pair.right;
    }
    
    public static implicit operator LSystemRule((string left, string right) pair)
    {
        return new LSystemRule(pair);
    }
}
