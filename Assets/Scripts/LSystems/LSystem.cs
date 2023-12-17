using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using Random = UnityEngine.Random;

public class LSystem : MonoBehaviour
{
    Vector3 position = Vector3.zero;
    Vector3 direction = Vector3.right;

    Stack<Vector3> positionStack;
    Stack<Vector3> directionStack;

    [Header("LSystem Parameters")]
    [SerializeField, Range(0, 7)] int depth = 4;
    [SerializeField, Range(0f, 360f)] float deltaRoll = 90;
    [SerializeField, Range(0f, 360f)] float deltaPitch = 90;
    [SerializeField, Range(0f, 360f)] float deltaYaw = 90;
    [SerializeField, Range(0f, 2f)] float deltaLength = 1;
    [SerializeField] string axiom;
    [SerializeField] List<SerializedTuple<string, string>> rules;
    [SerializeField] SerializedDict<string, FunctionEnum> functions;

    [Header("Style Parameters")]
    [SerializeField] GameObject prefabLeaf;
    [SerializeField] GameObject prefabLog;

    public void SetDepth(int depth)
    {
        this.depth = depth;
    }

    void Start() {
        positionStack = new Stack<Vector3>();
        directionStack = new Stack<Vector3>();
        position = transform.position;
        direction = Vector3.up;
        
        string state = axiom;
        for (int i = 0; i < depth; ++i) {
            state = Process(state);
        }

        // Gizmos.color = Color.green;
        Interpretation(state);
    }

    void OnDrawGizmos() {
        
    }

    // ========================= //
    // === LSystem Evolution === //
    // ========================= //

    string Process(string currentState) {
        string nextState = "";
        foreach (char symbol in currentState) {
            nextState += ApplyRules(symbol.ToString());
        }
        return nextState;
    }

    string ApplyRules(string symbol) {
        foreach (SerializedTuple<string, string> rule in rules) {
            if (rule._item1 == symbol)
                return rule._item2;
        }
        return symbol;
    }

    // ============================== //
    // === LSystem Representation === //
    // ============================== //

    void Interpretation(string state) {
        foreach (char symbol in state) {
            FunctionEnum functionEnum = functions[symbol.ToString()];
            switch (functionEnum) {
            case FunctionEnum.DrawLine:
                DrawLine(deltaLength);
                break;
            case FunctionEnum.Roll:
                Roll(deltaRoll);
                break;
            case FunctionEnum.AntiRoll:
                Roll(-deltaRoll);
                break;
            case FunctionEnum.PushContext:
                PushContext();
                break;
            case FunctionEnum.PopContext:
                PopContext();
                break;
            case FunctionEnum.Pitch:
                Pitch(deltaPitch);
                break;
            case FunctionEnum.AntiPitch:
                Pitch(-deltaPitch);
                break;
            case FunctionEnum.Yaw:
                Yaw(deltaYaw);
                break;
            case FunctionEnum.AntiYaw:
                Yaw(-deltaYaw);
                break;
            case FunctionEnum.DrawLeaf:
                DrawLeaf();
                break;
            case FunctionEnum.DrawLog:
                DrawLog(deltaLength);
                break;
            }
        }
    }

    // Availables functions
    void PushContext() {
        positionStack.Push(position);
        directionStack.Push(direction);
    }

    void PopContext() {
        position = positionStack.Pop();
        direction = directionStack.Pop();
    }

    void Roll(float angle) {
        direction = Quaternion.Euler(0, 0, angle) * direction;
    }

    void Pitch(float angle) {
        direction = Quaternion.Euler(angle, 0, 0) * direction;
    }

    void Yaw(float angle) {
        direction = Quaternion.Euler(0, angle, 0) * direction;
    }

    void DrawLine(float length) {
        Gizmos.DrawLine(position, position + direction * length);
        position += direction * length;
    }

    void DrawLog(float length) {
        GameObject log = Instantiate(
            prefabLog,
            position,
            Quaternion.LookRotation(direction),
            transform
        );
        Vector3 oldScale = log.transform.localScale;
        log.transform.localScale = new Vector3(oldScale.x, oldScale.y, length);
        position += direction * length;
    }

    // TODO random? offset?
    void DrawLeaf() {
        float size = Random.Range(0.5f, 1.5f);
        GameObject leaf = Instantiate(prefabLeaf, position, Quaternion.identity, transform);
        leaf.transform.localScale = Vector3.one * size;
    }
}

[Serializable]
enum FunctionEnum {
    DrawLine,
    Roll,
    AntiRoll,
    Pitch,
    AntiPitch,
    Yaw,
    AntiYaw,
    PopContext,
    PushContext,
    DrawLeaf,
    DrawLog
}