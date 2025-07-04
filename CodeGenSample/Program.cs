// See https://aka.ms/new-console-template for more information

using AppSettings;
using CodeGenSample;
using CodeGenSample.Entities;

using ENUMS;
using MyGeneration;
using Nextended.CodeGen.Generated;
using Nextended.Core.Attributes;



//ComUserLevel ds;
IMyUserDto c = MappingExtensions.ToMyDto(new User() {Name = "Herbert", Address = new Address() {City = "Bremen"}});
var city = c.ThatUserAddress.City;
var id = ComGuids.IdAddressDto;

ServerConfiguration sc;


c.ThatUserAddress = new AddressDto() {City = "HH"};

//var x = c.ThatUserAddress.YG;

Console.WriteLine($"{c.ThatUserAddress.HelloWorld}....{c.ThatUserAddress.GetSomething()}");

