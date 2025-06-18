using Nextended.CodeGen.Attributes;
using Nextended.Core.Attributes;

namespace CodeGenSample;

[AutoGenerateCom(Prefix = "My", Suffix = "Dto", GenericParameterTypes = new []{"string", "int"})]
public class User //<T>
{
    //public T Id { get; set; }
    public string Name { get; set; }
    
    [ComIgnore] 
    public string SecretDb { get; set; }
    
    [ComPropertySetting(PropertyName = "ThatUserAddress" //, Type = typeof(string)
    )]
    public Address Address { get; set; }
    
    [ComPropertySetting(PropertyName = "UserLevel")]
    public UserLevel? Level { get; set; }
}


[AutoGenerateCom(ToMethodName = "ToDto")]
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}

[AutoGenerateCom]
public enum UserLevel
{
    Guest,
    User,
    Admin,
    [ComIgnore]
    ServerAdmin,
}


public interface IInterface
{
    
}