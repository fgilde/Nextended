# Nextended
This Libraray was updated to .net7 and renamed from old "nExt" to Nextended

Old Nuget Package: https://www.nuget.org/packages/nExt.Core/ (No more updated)

Source for this package: https://github.com/fgilde/Nextended

Nuget Package https://www.nuget.org/packages/Nextended.Core/

# Description

This Library provides great and usefull Extension Methods, Small Helpers and Great usefull Types 

-Types
  - BaseId (Type for generic but Typed Id)
  - Money (Type to have a decimal working perfectly as Money type)
  - Date (A date type without time)
  - SuperType (a generic entity type that has a relationship with one or more subtypes)
  
-Extensions 
  - ClassMappingExtensions
  - AssemblyExtensions
  - CacheExtensions
  - DateTimeExtensions
  - EnumerableExtensions
  - ExceptionExtensions
  - GuidExtensions
  - JObjectExtensions
  - MemberInfoExtensions
  - NotificationExtensions
  - NumericExtensions
  - ObjectExtensions
  - StringExtensions
  - TaskExtensions
  - TypeExtensions
  - UrlExtensions

Also this Package provide a very fast and Very easy to use classmapper. 
But this doesnt mean that this classmapper is not powerfull. If you want you can register classmappers with different assignements and conversion behaviors. But If you dont need you can start very quick ans easy for example.

```
Instance.MapTo<IInstanceInterfaceDto>()
```
You have possibilities to SetUp behavior of mapping for each map, or bind to mapper instance or register global

```
         var inputObject = new Object1 { ReportId = 8, ReportId2 = 9, IsMale = true, Age = 23, Name = "Hans", Adress = new Adress { Number = "19", Street = "Am Sonnenhang" } };
            var settings = ClassMappingSettings.Default.IgnoreProperties<Object1>(o => o.Age, o => o.ExtraShit)
                .IgnoreProperties<Adress>(adress2 => adress2.Street);
            inputObject.MapTo<Object2>(settings);
```

More documentation and features are comming soon
