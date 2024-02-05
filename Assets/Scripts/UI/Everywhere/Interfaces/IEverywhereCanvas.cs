public interface IEverywhereCanvas
{
    public bool Active { get; set; }
    public void ResetCanvas();
    public void OnDisconnect();
}
