using System;

[Serializable]
class SerializedTuple<TItem1, TItem2>
{
    public TItem1 _item1;
    public TItem2 _item2;

    public SerializedTuple(TItem1 item1, TItem2 item2) {
        _item1 = item1;
        _item2 = item2;
    }
}
