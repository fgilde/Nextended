// See https://aka.ms/new-console-template for more information

using CodeGenSample;
using CodeGenSample.Entities;

using ENUMS;
using MyGeneration;
using Nextended.CodeGen.Generated;
using Nextended.Core.Attributes;
using SlamHarder;


//ComUserLevel ds;
IMyUserDto c = MappingExtensions.ToMyDto(new User() {Name = "Herbert", Address = new Address() {City = "Bremen"}});
var city = c.ThatUserAddress.City;
var id = ComGuids.IdAddressDto;
MyMyRootType r;
c.ThatUserAddress = new AddressDto() {City = "HH"};

//var x = c.ThatUserAddress.YG;

Console.WriteLine($"{c.ThatUserAddress.HelloWorld}....{c.ThatUserAddress.GetSomething()}");

