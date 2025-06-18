using Nextended.CodeGen.Attributes;
using Nextended.Core.Attributes;

namespace CodeGenSample;

[AutoGenerateCom(Prefix = "My", Suffix = "Dto")]
public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    
    [ComIgnore] 
    public string SecretDb { get; set; }
    
    [ComPropertySetting(PropertyName = "ThatUserAddress")]
    public Address Address { get; set; }
    
    [ComPropertySetting(PropertyName = "UserLevel")]
    public UserLevel Level { get; set; }
}


[AutoGenerateCom]
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