namespace ScrapperHttpFunction.ResultContainer;

public class ContainerResult
{
    public Exception Exception { get; set; }
    public bool Success { get; set; }
}

public class ContainerResult<T> : ContainerResult
{
    public T Value { get; set; }
}
