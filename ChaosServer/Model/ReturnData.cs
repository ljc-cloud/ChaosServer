namespace ChaosServer.Model;

public class ReturnData<T>
{
    public bool success;
    public string errorMessage;
    public string successMessage;

    public T data;
}