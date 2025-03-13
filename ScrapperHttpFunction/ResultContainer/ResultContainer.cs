namespace ScrapperHttpFunction.ResultContainer;

public class ContainerResult
{
    public bool Success { get; set; }
}

public class ContainerResult<T> : ContainerResult
{
    public T Value { get; set; }
}
