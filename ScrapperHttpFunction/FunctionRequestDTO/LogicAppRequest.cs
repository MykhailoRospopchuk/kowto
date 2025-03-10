namespace ScrapperHttpFunction.FunctionRequestDTO;

public class LogicAppRequest<T>
{
    public string Title { get; set; }
    public T Content { get; set; }
}