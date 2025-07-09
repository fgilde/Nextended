// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using AppSettings;
using CodeGenSample;
using CodeGenSample.Entities;

using ENUMS;

using MyGeneration;
using Nextended.CodeGen.Generated;


//ComUserLevel ds;
IMyUserDto c = MappingExtensions.ToMyDto(new User() {Name = "Herbert", Address = new Address() {City = "Bremen"}});
var city = c.ThatUserAddress.City;
var id = ComGuids.IdAddressDto;


ServerConfiguration sc = new();
//var readAllText = File.ReadAllText(Path.Combine("Sources", "appsettings.json"));
//ServerConfiguration configFromJson = JsonSerializer.Deserialize<ServerConfiguration>(readAllText);



c.ThatUserAddress = new AddressDto() {City = "HH"};

//var x = c.ThatUserAddress.YG;

Console.WriteLine($"{c.ThatUserAddress.HelloWorld}....{c.ThatUserAddress.GetSomething()}");

