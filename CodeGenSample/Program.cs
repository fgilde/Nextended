// See https://aka.ms/new-console-template for more information

using CodeGenSample;
using CodeGenSample.Entities;
using MyGeneration;
using N.CG.AutoGen;
using Nextended.Core.Attributes;


//ComUserLevel ds;
IMyUserDto c = MappingExtensions.ToMyDto(new User() {Name = "Herbert", Address = new Address() {City = "Bremen"}});
var city = c.ThatUserAddress.City;


c.ThatUserAddress = new AddressDto() {City = "HH"};

//var x = c.ThatUserAddress.YG;

Console.WriteLine($"{c.ThatUserAddress.HelloWorld}....{c.ThatUserAddress.GetSomething()}");

