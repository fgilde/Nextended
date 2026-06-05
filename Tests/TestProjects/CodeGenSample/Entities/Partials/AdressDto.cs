using CodeGenSample.Entities.Base;

namespace MyGeneration;

public partial interface IAddressDto : IHelloWorld
{
    string GetSomething() => "123";
}