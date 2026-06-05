using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;
using Nextended.Core.Tests.classes;
using Nextended.Core.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;

namespace Nextended.Core.Tests
{
    class OneClass
    {
        public string Name { get; set; }
        public string Amount { get; set; }

    }

    class TwoClass
    {
        public string Name { get; set; }
        public Money Amount { get; set; }

    }

    [TestClass]
    public class ClassMappingTests
    {


        [TestMethod]
        public void ToDoubleTests()
        {
            ClassMappingSettings setts2 = ClassMappingSettings.Default.AddAllLoadedTypeConverters();
            
            var dateTime = new DateTime(2022, 12, 24, 10, 10, 10);
           

            double ticks = dateTime.MapTo<double>(setts2);

            Assert.IsTrue(ticks == dateTime.Ticks);

            var timeSpan = new TimeSpan(1, 2, 3, 4, 5);
            ticks = timeSpan.MapTo<double>(setts2);
            Assert.IsTrue(ticks == timeSpan.Ticks);

            var dateOnly = new DateOnly(2022, 12, 24);
            ticks = dateOnly.MapTo<double>(setts2);
            Assert.IsTrue(ticks == dateOnly.DayNumber);

            var timeOnly = new TimeOnly(10, 33, 12);
            ticks = timeOnly.MapTo<double>(setts2);
            Assert.IsTrue(ticks == timeOnly.ToTimeSpan().Ticks);

        }


        [TestMethod]
        public void MoneyPropTest()
        {
            var c = new OneClass() { Name = "Hans P", Amount = "213,23" };
            var two = c.MapTo<TwoClass>();
            Assert.AreEqual(c.Name, two.Name);
            Assert.AreEqual(213.23m, two.Amount.Amount);
        }

        [TestMethod]
        public void TestMapDateOnly()
        {
            var dateTime = new DateTime(2022, 12, 24, 10, 10, 10);
            var dateOnly = dateTime.MapTo<DateOnly>();
            
            Assert.IsNotNull(dateOnly);
            Assert.IsTrue(dateOnly.Day == 24);
            Assert.IsTrue(dateOnly.Month == 12);
            Assert.IsTrue(dateOnly.Year == 2022);

            var date = dateOnly.MapTo<DateTime>();
            Assert.IsNotNull(date);
            Assert.IsTrue(date.Day == 24);
            Assert.IsTrue(date.Month == 12);
            Assert.IsTrue(date.Year == 2022);
        }

        [TestMethod]
        public void TestMapTimeOnly()
        {
            var dateTime = new DateTime(2022, 12, 24, 10, 33, 12);
            var timeOnly = dateTime.MapTo<TimeOnly>();

            Assert.IsNotNull(timeOnly);
            Assert.IsTrue(timeOnly.Hour == 10);
            Assert.IsTrue(timeOnly.Minute == 33);
            Assert.IsTrue(timeOnly.Second == 12);

            var date = timeOnly.MapTo<DateTime>();
            Assert.IsNotNull(date);
            Assert.IsTrue(date.Hour == 10);
            Assert.IsTrue(date.Minute == 33);
            Assert.IsTrue(date.Second == 12);


            var span = timeOnly.MapTo<TimeSpan>();
            Assert.IsNotNull(span);
            Assert.IsTrue(span.Hours == 10);
            Assert.IsTrue(span.Minutes == 33);
            Assert.IsTrue(span.Seconds == 12);

            timeOnly = span.MapTo<TimeOnly>();
            Assert.IsNotNull(timeOnly);
            Assert.IsTrue(timeOnly.Hour == 10);
            Assert.IsTrue(timeOnly.Minute == 33);
            Assert.IsTrue(timeOnly.Second == 12);
        }

        [TestMethod]
        public void TestParamCountMissMatch()
        {
            var dto = new ProductDto() {Barcode = "XYZ", Brand = "MyBnrand", BrandId = 2, Description = "Hello", Id = 3, Name = "A product"};
            var res = dto.MapTo<Product>(ClassMappingSettings.Default.Set(s => s.IgnoreExceptions = true));
        }

        [TestMethod]
        public void TestSystemInterface()
        {
            IList<string> list = typeof(IList<string>).CreateInstance<IList<string>>(checkCyclingDependencies:false);
            list.Add("Hello");
            Assert.IsTrue(list.Count == 1);


            var ol = typeof(IList<object>).CreateInstance<IList<object>>(checkCyclingDependencies: false);
            ol.Add(new object());
            Assert.IsTrue(ol.Count == 1);

        }

        [TestMethod]
        public void TestInterface()
        {
            var r = typeof(IDateTestRange).CreateInstance<IDateTestRange>(checkCyclingDependencies:false);
            Assert.IsNotNull(r.EndDate);
            Assert.IsNotNull(r.StartDate);
            typeof(INotImplementedInterface).CreateInstance<INotImplementedInterface>(checkCyclingDependencies: false);
        }

        [TestMethod]
        public void ToEnum()
        {
            var e = 0x00000004.MapTo<DateTimeStyles>();
            Assert.AreEqual(DateTimeStyles.AllowInnerWhite, e);
            var e2 = "AllowInnerWhite".MapTo<DateTimeStyles>();
            Assert.AreEqual(DateTimeStyles.AllowInnerWhite, e2);
        }

        [TestMethod]
        public void TestString()
        {
            var testString = "Hallo";
            var s = testString.MapTo<char[]>();
            var list = s.MapTo<List<char>>();
            var list2 = testString.MapTo<List<char>>();
            Assert.IsNotNull(s);
            Assert.AreEqual(testString.Length, s.Length);
            Assert.AreEqual(testString.Length, list.Count);
            Assert.AreEqual(testString.Length, list2.Count);
        }

        [TestMethod]
        public void TestGInt()
        {
            Guid newGuid = Guid.NewGuid();
            var mapTo = newGuid.MapTo<uint>();
            Assert.IsTrue(mapTo > 0);
            var guid = mapTo.MapTo<Guid>();
            Assert.IsTrue(guid != Guid.Empty);
            int i = guid.MapTo<int>();
            Assert.AreEqual(i, (int)mapTo);
        }

        [TestMethod]
        public void TestConvertWithBaseTypeMapOverride()
        {
            var test = new Test();
            ClassMappingSettings settings2 = ClassMappingSettings.Default;
            settings2.AddConverter<string, DateTime>(DateTime.Parse);
            var test2 = test.MapTo<Test2>(settings2);
            Assert.AreEqual(DateTime.Now.AddDays(-2).Date, test2.Date.Date);

            //ClassMappingSettings settings = ClassMappingSettings.Default;
            //ExceptionAssert.Throws<AggregateException>(() => test.MapTo<Test2>(settings));
        }

        [TestMethod]
        public void TestConvertWithInheritedValuesAndInputs()
        {
            var textCell = new CellObject { CellType = CellType.Text, Value = new TextValue { Value = "Ich bin ein Text" } };

            ClassMappingSettings settings = ClassMappingSettings.Default;
            settings.AddConverter<CellValue, MyComTextValue>();
            MyComObject comObject = textCell.MapTo<MyComObject>(settings);
            Assert.IsFalse(comObject.Value is MyComTextValue);


            ClassMappingSettings settings2 = ClassMappingSettings.Default;
            settings2.AddConverter<CellValue, MyComTextValue>(null, true);
            comObject = textCell.MapTo<MyComObject>(settings2);
            Assert.IsTrue(comObject.Value is MyComTextValue);
        }

        [TestMethod]
        public void TestConvertWithInheritedValues()
        {


            var textCell = new CellObject { CellType = CellType.Text, Value = new TextValue { Value = "Ich bin ein Text" } };
            var doubleCell = new CellObject { CellType = CellType.Text, Value = new DoubleValue { Value = 100.42d } };
            var warnCell = new CellObject { CellType = CellType.Text, Value = new DoubleValueWarnlight { Value = 21.42d, Warnlight = WarnlightType.Red } };

            ClassMappingSettings settings = ClassMappingSettings.Default;
            settings.AddConverter<TextValue, MyComTextValue>();
            settings.AddConverter<DoubleValue, MyComDoubleValue>();
            settings.AddConverter<DoubleValueWarnlight, MyComDoubleValue>();

            MyComObject myComObject = textCell.MapTo<MyComObject>(settings);
            MyComObject comObject = doubleCell.MapTo<MyComObject>(settings);
            MyComObject comObject2 = warnCell.MapTo<MyComObject>(settings);

            Assert.IsTrue(comObject.Value is MyComDoubleValue);
            Assert.IsTrue(comObject2.Value is MyComDoubleValue);
            Assert.IsTrue(myComObject.Value is MyComTextValue);
        }



        [TestMethod]
        public void SimpleObjectTestsWithDifferentTypes()
        {
            var inputObject = new Object1 { ReportId = 8, ReportId2 = 9, IsMale = true, Age = 23, Name = "Hans", Adress = new Adress { Number = "19", Street = "Am Sonnenhang" } };
            Object2 outputObject = inputObject.MapTo<Object1, Object2>((object2, object1) => object2.Gender = object1.IsMale,
                                                                              (object2, object1) => object2.Adress.PostalCode = 1234);

            Assert.AreEqual(inputObject.IsMale, outputObject.Gender);

            Assert.AreEqual(inputObject.MyInt.Number, outputObject.MyInt);
            Assert.AreEqual(inputObject.DaInt, outputObject.DaInt.Number);
            Assert.AreEqual(inputObject.Id, outputObject.Id.ToString());

            Assert.AreEqual(inputObject.Age.ToString(CultureInfo.InvariantCulture), outputObject.Age);
            Assert.AreEqual(inputObject.ReportId, (int)outputObject.ReportId);
            Assert.AreEqual(inputObject.ReportId2, (uint)outputObject.ReportId2);

            Assert.AreEqual(inputObject.Adress.Number, outputObject.Adress.Number.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(inputObject.Adress.Street, outputObject.Adress.Street);
            Assert.AreEqual(1234, outputObject.Adress.PostalCode);
            Assert.AreEqual("23", outputObject.Age);

            Assert.AreEqual(inputObject.ExtraShit.Number, outputObject.ExtraShit.Number);

            Assert.AreNotEqual(inputObject.GetType(), outputObject.GetType());
            Assert.AreNotEqual(inputObject.Adress.GetType(), outputObject.Adress.GetType());
            Assert.AreNotEqual(inputObject.ExtraShit.GetType(), outputObject.ExtraShit.GetType());
        }

        [TestMethod]
        public void PrivateFieldMapTest()
        {
            ClassMappingSettings classMappingSettings = ClassMappingSettings.Default.Set(settings => settings.IncludePrivateFields = false);
            var inputObject = new Object1 { ReportId = 8, ReportId2 = 9, IsMale = true, Age = 23, Name = "Hans", Adress = new Adress { Number = "19", Street = "Am Sonnenhang" } };
            Object2 outputObject = inputObject.MapTo<Object1, Object2>(classMappingSettings, (object2, object1) => object2.Gender = object1.IsMale,
                                                                              (object2, object1) => object2.Adress.PostalCode = 1234);

            Assert.IsNull(outputObject.GetPrivateValueResult());
            Assert.AreEqual(Guid.Empty, outputObject.LevelIdValue);

            classMappingSettings = ClassMappingSettings.Default.Set(settings => settings.IncludePrivateFields = true);
            outputObject = inputObject.MapTo<Object1, Object2>(classMappingSettings, (object2, object1) => object2.Gender = object1.IsMale,
                                                                              (object2, object1) => object2.Adress.PostalCode = 1234);


            Assert.AreNotEqual(Guid.Empty, outputObject.LevelIdValue);
            Assert.IsNotNull(outputObject.GetPrivateValueResult());
            Assert.AreEqual(inputObject.GetPrivateValueResult(), outputObject.GetPrivateValueResult());
            Assert.AreEqual(outputObject.Level, outputObject.LevelIdValue);
            Assert.AreEqual(1234, outputObject.Adress.PostalCode);
            Assert.AreEqual(inputObject.Adresses.Count(), outputObject.Adresses.Length);

        }

        [TestMethod]
        public void TestAnonymusMap()
        {
            var date = DateTime.Now;
            var extraShit = new { Time = date, Number = 42 }.MapTo<IExtraShit>();
            Assert.IsNotNull(extraShit);
            Assert.AreEqual(date, extraShit.Time);
            Assert.AreEqual(42, extraShit.Number);
        }

        [TestMethod]
        public void TestValueTypes()
        {
            const string s = "123,44";
            double d = s.MapTo<double>();
            Assert.AreEqual(123.44, d);

            const int x = 9;
            MyInt mx = x.MapTo<MyInt>();
            int i = mx.MapTo<int>();

            Assert.AreEqual(9, mx.Number);
            Assert.AreEqual(9, i);
        }


        [TestMethod]
        public void SimpleAbstractCoverUpTest()
        {
            var info = new MyInfo();
            MyInfoAbstract result = info.MapTo<MyInfoAbstract>(ClassMappingSettings.Default.Set(settings => settings.CoverUpAbstractMembers = true));
            Assert.IsNull(result.Adress);
            var prop = result.GetType().GetProperties();
            Assert.AreEqual(2, prop.Count());
            Assert.IsNotNull(prop[0].GetValue(result, null));
            Assert.IsNull(prop[1].GetValue(result, null));
        }

        [TestMethod]
        public void SimpleAbstractTest()
        {
            var info = new MyInfo();
            MyInfoAbstract result = info.MapTo<MyInfoAbstract>();
            var dataConverted = JsonConvert.SerializeObject(result);
            Assert.IsTrue(dataConverted.Contains("Am Sonnenhang"));
        }

        [TestMethod]
        public void ListDirectMappingTest()
        {
            var list = new List<string> { "1", "2", "3" };
            int[] linqConverted = list.Select(s => Convert.ToInt32(s)).ToArray();
            int[] intArray = list.MapTo<int[]>();
            for (int index = 0; index < intArray.Length; index++)
            {
                var element2 = intArray[index];
                var element = linqConverted[index];
                Assert.AreEqual(element, element2);
            }
        }

        [TestMethod]
        public void ListMappingTest()
        {
            var classWithList = new SomeClassWithList { Name = "MyListClass" };
            var other = classWithList.MapTo<SomeOtherClassWithList>(ClassMappingSettings.Default.Set(settings => settings.ShouldEnumerateListsAsync = true));

            Assert.AreEqual(classWithList.Dictionary.Count, other.Dictionary.Count);
            Assert.AreEqual(classWithList.ElementArray.Length, other.ElementArray.Length);
            Assert.AreEqual(classWithList.Elements.Count(), other.Elements.Count());
        }

        [TestMethod]
        public void TestPropertyIgnores()
        {
            var inputObject = new Object1 { ReportId = 8, ReportId2 = 9, IsMale = true, Age = 23, Name = "Hans", Adress = new Adress { Number = "19", Street = "Am Sonnenhang" } };
            var outputObject = new ClassMapper().SetSettings(ClassMappingSettings.Default.IgnoreProperties<Object1>(o => o.Age, o => o.ExtraShit, o => o.Adress.Street))
                .Map<Object1, Object2>(inputObject);

            Assert.IsTrue(string.IsNullOrEmpty(outputObject.Age));
            Assert.IsTrue(string.IsNullOrEmpty(outputObject.Adress.Street));
            Assert.IsTrue(outputObject.ExtraShit == null);

            Assert.AreNotEqual(inputObject.IsMale, outputObject.Gender);
            Assert.AreEqual(inputObject.ReportId, (int)outputObject.ReportId);
            Assert.AreEqual(inputObject.ReportId2, (uint)outputObject.ReportId2);

            Assert.AreEqual(inputObject.Adress.Number, outputObject.Adress.Number.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(0, outputObject.Adress.PostalCode);
        }

        [TestMethod]
        public void TestPropertyIgnores2()
        {
            var inputObject = new Object1 { ReportId = 8, ReportId2 = 9, IsMale = true, Age = 23, Name = "Hans", Adress = new Adress { Number = "19", Street = "Am Sonnenhang" } };
            var settings = ClassMappingSettings.Default.IgnoreProperties<Object1>(o => o.Age, o => o.ExtraShit);

            settings.IgnoreProperties<Adress>(adress2 => adress2.Street);
            var outputObject = new ClassMapper().SetSettings(settings)
                .Map<Object1, Object2>(inputObject);

            Assert.IsTrue(string.IsNullOrEmpty(outputObject.Age));
            Assert.IsTrue(string.IsNullOrEmpty(outputObject.Adress.Street));
            Assert.IsTrue(outputObject.ExtraShit == null);

            Assert.AreNotEqual(inputObject.IsMale, outputObject.Gender);
            Assert.AreEqual(inputObject.ReportId, (int)outputObject.ReportId);
            Assert.AreEqual(inputObject.ReportId2, (uint)outputObject.ReportId2);

            Assert.AreEqual(inputObject.Adress.Number, outputObject.Adress.Number.ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual(0, outputObject.Adress.PostalCode);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public void CrazyConversionTestExpectedException()
        {
            var classMappingSettings = ClassMappingSettings.Default.Set(settings => settings.IgnoreExceptions = false,
                settings => settings.AllowGuidConversion = false);
            var myInitialObject = new MyCustomObject();
            var myNewCustomObject = myInitialObject.MapTo<MyNewCustomObject>(classMappingSettings);
            Assert.IsNull(myNewCustomObject);
        }

        [TestMethod]
        public void CrazyConversionTestPartialFilled()
        {
            var classMappingSettings = ClassMappingSettings.Default.Set(settings => settings.IgnoreExceptions = true,
                settings => settings.AllowGuidConversion = false);
            var myInitialObject = new MyCustomObject();
            var myNewCustomObject = myInitialObject.MapTo<MyNewCustomObject>(classMappingSettings);
            Assert.IsNotNull(myNewCustomObject);
            Assert.AreEqual(myInitialObject.AssignableKey, myNewCustomObject.AssignableKey.ToString());
            Assert.AreEqual(Guid.Empty, myNewCustomObject.NotAssignableKey);
            Assert.AreEqual(Guid.Empty, myNewCustomObject.Number);
            Assert.AreEqual(default(int), myNewCustomObject.Id);
        }

        [TestMethod]
        public void SimpleGuidConversionTest()
        {
            const int myInt = int.MaxValue;
            Guid id = myInt.ToGuid();
            var intAgain = id.ToInt();
            Assert.AreEqual(myInt, intAgain);

            const long alalalalaLong = Int64.MaxValue;
            id = alalalalaLong.ToGuid();
            long longAgain = id.ToInt64();
            Assert.AreEqual(alalalalaLong, longAgain);
        }

        [TestMethod]
        public void TestSettingsSet()
        {
            var classMappingSettings2 = ClassMappingSettings.Default.Set(settings => settings.IncludePrivateFields, true);
            var classMappingSettings3 = ClassMappingSettings.Default.Set(settings => settings.IncludePrivateFields = false);
            Assert.AreEqual(true, classMappingSettings2.IncludePrivateFields);
            classMappingSettings2.Set(settings => settings.IncludePrivateFields, false);
            Assert.AreEqual(false, classMappingSettings2.IncludePrivateFields);
            Assert.AreEqual(false, classMappingSettings3.IncludePrivateFields);
            classMappingSettings3 = ClassMappingSettings.Default.Set(settings => settings.IncludePrivateFields = true);
            Assert.AreEqual(true, classMappingSettings3.IncludePrivateFields);

        }

        [TestMethod]
        public void CrazyConversionTestCompleteFilled()
        {
            var classMappingSettings = ClassMappingSettings.Default.Set(settings => settings.IgnoreExceptions = false,
                settings => settings.AllowGuidConversion = true);
            var myInitialObject = new MyCustomObject();
            var myNewCustomObject = myInitialObject.MapTo<MyNewCustomObject>(classMappingSettings);
            Assert.IsNotNull(myNewCustomObject);
            Assert.AreEqual(myInitialObject.AssignableKey, myNewCustomObject.AssignableKey.ToString());
            Assert.AreEqual(myInitialObject.NotAssignableKey.GetHashCode().ToGuid(), myNewCustomObject.NotAssignableKey);
            Assert.AreEqual(myInitialObject.Number.ToGuid(), myNewCustomObject.Number);
            Assert.IsTrue(myNewCustomObject.Id != default(int));

            // Convert Back
            var myNewInitialObject = myNewCustomObject.MapTo<MyCustomObject>(classMappingSettings);
            Assert.AreEqual(myInitialObject.AssignableKey, myNewInitialObject.AssignableKey);
            Assert.AreEqual(myInitialObject.Number, myNewInitialObject.Number);
        }

        [TestMethod]
        public void TestDefaultValueAssignment()
        {
            var classMappingSettings = ClassMappingSettings.Default.Set(settings => settings.IgnoreExceptions = false,
                settings => settings.AllowGuidConversion = true,
                settings => settings.DefaultValueTypeValuesAsNullForNonValueTypes = false);
            var myInitialObject = new MyCustomObject();
            var myNewCustomObject = myInitialObject.MapTo<MyNewCustomObject>(classMappingSettings);
            Assert.IsNotNull(myNewCustomObject.AnotherNumber);
            Assert.AreEqual(myInitialObject.AnotherNumber, myNewCustomObject.AnotherNumber.Number);

            classMappingSettings.DefaultValueTypeValuesAsNullForNonValueTypes = true;
            var myNewNewCustomObject = myInitialObject.MapTo<MyNewCustomObject>(classMappingSettings);
            Assert.IsNull(myNewNewCustomObject.AnotherNumber);
        }

        //[TestMethod]
        //public void PeriodDateConverterTest()
        //{
        //	var dateRange = new MyDateRange();
        //	var myPeriodDateRange = dateRange.MapTo<MyPeriodDateRange>();
        //	Assert.AreEqual(dateRange.StartDate.Date, myPeriodDateRange.StartDate.ToDateTime());
        //	Assert.AreEqual(dateRange.EndDate.Date, myPeriodDateRange.EndDate.ToDateTime());

        //	var periodDate1 = new PeriodDate(2014, 8, 23);
        //	var mappableString = periodDate1.ToString();
        //	var periodDate = mappableString.MapTo<PeriodDate>(ClassMappingSettings.Default.AddAllLoadedTypeConverters());
        //	Assert.IsNotNull(periodDate);
        //	var mappedString = periodDate.MapTo<string>();
        //	Assert.AreEqual(periodDate.ToString(), mappedString);

        //	var pdate = DateTime.Now.MapTo<PeriodDate>();
        //	Assert.AreEqual(DateTime.Now.Date, pdate.ToDateTime());

        //	var date = periodDate1.MapTo<DateTime>(new PeriodDateConverter());
        //	Assert.AreEqual(date.Date, periodDate1.ToDateTime());
        //}

        [TestMethod]
        public void FindImplementingTypeTest()
        {
            var type = ReflectionHelper.FindImplementingType(typeof(IDateTestRange));
            Assert.AreEqual(typeof(MyDateRange), type);
        }

        [TestMethod]
        public void DictionaryToObject()
        {
            var dict = new Dictionary<string, DateTime> { { "StartDate", DateTime.Now }, { "EndDate", DateTime.Now } };
            var myDateRange = dict.MapTo<IDateTestRange>();
            Assert.IsNotNull(myDateRange);
            Assert.AreEqual(typeof(MyDateRange), myDateRange.GetType());
            Assert.AreEqual(dict["StartDate"], myDateRange.StartDate);
            Assert.AreEqual(dict["EndDate"], myDateRange.EndDate);
        }

        [TestMethod]
        public void TestEnumMapping()
        {
            var c1 = new Class1 { Mode = MyEnumMode.Fast };
            Class2 c2 = c1.MapTo<Class2>();
            Assert.AreEqual("schnell", c2.Mode);
            Class3 c3 = c1.MapTo<Class3>();
            Assert.AreEqual(2, c3.Mode);

            Class1 newClass1 = c3.MapTo<Class1>();
            Assert.AreEqual(MyEnumMode.Fast, newClass1.Mode);

            Class1 newClass1a = c2.MapTo<Class1>();
            Assert.AreEqual(MyEnumMode.Fast, newClass1a.Mode);

            var class2 = new Class2 { Mode = "suPerFaSt" };
            Class1 c = class2.MapTo<Class1>();
            Assert.AreEqual(MyEnumMode.SuperFast, c.Mode);
            ExceptionAssert.Throws<AggregateException>(() => class2.MapTo<Class1>(ClassMappingSettings.Default.Set(settings => settings.MatchCaseForEnumNameConversion, true)));

        }

        [TestMethod]
        public void TestEnumWithAttributeMapping()
        {
            var class2 = new Class2 { Mode = "fast" };
            Class1 c = class2.MapTo<Class1>();
            Assert.AreEqual(MyEnumMode.Fast, c.Mode);

            var class23 = new Class2 { Mode = "schnell" };
            var r = class23.MapTo<Class1>();
            Assert.AreEqual(MyEnumMode.Fast, r.Mode);

        }

        [TestMethod]
        public void TestWithDifferentPropNames()
        {
            var com = new TestCom();
            var settings = ClassMappingSettings.Default
                .AddConverter<string, DateTime>()
                .AddAssignment<TestCom, TestNormal>(testCom => testCom.Time, normal => normal.Date)
                .AddAssignment<TestCom, TestNormal>(testCom => testCom.Time, normal => normal.AuchDieTime);
            TestNormal result = com.MapTo<TestCom, TestNormal>(settings, (normal, testCom) => normal.TimeReverse = new String(testCom.Time.Reverse().ToArray()));
            Assert.AreEqual(com.Time, result.AuchDieTime);
            Assert.AreEqual(com.Time, result.Date.ToString());
            Assert.IsNotNull(result.TimeReverse);

            DateTime dt = result.AuchDieTime.MapTo<DateTime>();
            Assert.AreEqual(result.Date, dt);
        }

        [TestMethod]
        public void TestWithDifferentPropNames2()
        {
            var com = new TestCom();
            var settings = ClassMappingSettings.Default
                .AddConverter<string, DateTime>()
                .Assign<TestCom>(testCom => testCom.Time).To<TestNormal>(normal => normal.Date).And<TestNormal>(normal => normal.AuchDieTime)
                .Settings();
            TestNormal result = com.MapTo<TestNormal>(settings);
            Assert.AreEqual(com.Time, result.AuchDieTime);
            Assert.AreEqual(com.Time, result.Date.ToString());
        }

        [TestMethod]
        public void TestMapMultipleIds()
        {
            var com = new ComOrganisation();
            var settings = ClassMappingSettings.Default
                .AddConverter<ComId, OrganisationId>(id => id.Guid.MapTo<OrganisationId>())
                .AddConverter<ComId, int>(id => id.Id)
                .Assign<ComOrganisation>(c => c.Id).To<MyOrganisation>(o => o.Id).And<MyOrganisation>(o => o.DelphiId)
                .Settings();
            var result = com.MapTo<MyOrganisation>(settings);
            Assert.AreEqual(com.Id.Guid, result.Id.Id);
            Assert.AreEqual(com.Id.Id, result.DelphiId);
            Assert.AreEqual(com.Name, result.Name);


            IEnumerable<ComOrganisation> comOrganisations = Enumerable.Repeat(new ComOrganisation(), 200);
            IEnumerable<MyOrganisation> myOrganisations = comOrganisations.MapElementsTo<MyOrganisation>(settings);
            Assert.AreEqual(comOrganisations.Count(), myOrganisations.Count());
        }

        [TestMethod]
        public void TestOperator()
        {

            var info = new Info { Name = "Jens Uwe", Number = new MyInt { Number = 42 } };
            Info2 info2 = info.MapTo<Info2>();
            Assert.AreEqual(42, info2.Number);
            Assert.AreEqual(info.Name.ToCharArray().Length, info2.Name.Length);
            var newInfo1 = info2.MapTo<Info>();
            Assert.AreEqual(42, newInfo1.Number.Number);
            Assert.AreEqual(newInfo1.Name, newInfo1.Name);
            char[] chars = "Hallo".MapTo<char[]>();
            string hallo = chars.MapTo<string>();
            Assert.AreEqual("Hallo", hallo);
        }

        [TestMethod]
        public void TestGlobalConverters()
        {
            var com = new ComOrganisation();
            var settings = ClassMappingSettings.Default
                .AddConverter<ComId, OrganisationId>(id => id.Guid.MapTo<OrganisationId>())
                .AddConverter<ComId, int>(id => id.Id);
            var result = com.MapTo<MyOrganisation>(settings);
            Assert.AreEqual(com.Id.Guid, result.Id.Id);
            Assert.AreEqual(com.Name, result.Name);
            
            ClassMappingSettings.AddGlobalConverter<ComId, OrganisationId>(id => id.Guid.MapTo<OrganisationId>());
            ClassMappingSettings.AddGlobalConverter<ComId, int>(id => id.Id);
            com.MapTo<MyOrganisation>();
            Assert.AreEqual(com.Id.Guid, result.Id.Id);
            Assert.AreEqual(com.Name, result.Name);

            ClassMappingSettings.ClearGlobalConverters();
            ExceptionAssert.Throws<AggregateException>(() => com.MapTo<MyOrganisation>());

            var wrongResult = com.MapTo<MyOrganisation>(ClassMappingSettings.Default.Set(s => s.IgnoreExceptions = true));
            Assert.AreNotEqual(com?.Id?.Guid, wrongResult?.Id?.Id);
        }

        [TestMethod]
        public void TestCurrencyMapping()
        {

            var anonymousCurrencies = Currency.All.Select(currency => new
            {
                ISO = currency.IsoCode,
                currency.Name,
                currency.Regions,
                currency.Cultures,
                Description = currency.NativeName,
                currency.Symbol
            });

            var propInfo1 = anonymousCurrencies.First().GetType().GetProperty("ISO");
            var propInfo2 = anonymousCurrencies.First().GetType().GetProperty("Description");
            var settings = ClassMappingSettings.Default.
                AddAssignment<Currency>(propInfo1, currency => currency.IsoCode).
                AddAssignment<Currency>(propInfo2, currency => currency.NativeName);

            Currency[] mappedResult = anonymousCurrencies.MapElementsTo<Currency>(settings).ToArray();

            Assert.AreEqual(anonymousCurrencies.Count(), mappedResult.Length);
            Assert.AreEqual(anonymousCurrencies.First().ISO, mappedResult[0].IsoCode);
            Assert.AreEqual(anonymousCurrencies.First().Description, mappedResult[0].NativeName);
            Assert.AreEqual(anonymousCurrencies.First().Name, mappedResult[0].Name);
            Assert.AreEqual(anonymousCurrencies.First().Symbol, mappedResult[0].Symbol);
            Assert.AreEqual(anonymousCurrencies.First().Regions.Count(), mappedResult[0].Regions.Count());
            Assert.AreEqual(anonymousCurrencies.First().Cultures.Count(), mappedResult[0].Cultures.Count());
            Assert.AreEqual(anonymousCurrencies.First().Cultures.First(), mappedResult[0].Cultures.First());
            Assert.AreEqual(anonymousCurrencies.First().Regions.First(), mappedResult[0].Regions.First());
        }


    }

    #region Testklassen

    class Info2
    {
        public int Number;
        public char[] Name;
    }

    class Info
    {
        public MyInt Number;
        public string Name;
    }

    class ComOrganisation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public ComOrganisation()
        {
            Name = "Der Laden";
            Id = new ComId() { Guid = Guid.NewGuid(), Id = DateTime.Now.ToUnixTimeStamp() };
        }
        public string Name;
        public ComId Id;
    }

    class ComId
    {
        public int Id;
        public Guid Guid;
    }

    class MyOrganisation
    {
        public string Name;
        public OrganisationId Id;
        public int DelphiId;
    }


    class TestCom
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public TestCom()
        {
            Time = DateTime.Now.AddDays(-8).ToString();
        }

        public string Time { get; set; }
    }

    class TestNormal
    {
        public DateTime Date;
        public string AuchDieTime;
        public string TimeReverse;
    }

    #region Simple Test Klassen für type mapping

    internal class Test
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public Test()
        {
            Date = DateTime.Now.AddDays(-2).ToString();
            Period = "P1M";
        }

        public string Period { get; set; }
        public string Date { get; set; }
    }

    internal class Test2
    {
        public string Period { get; set; }
        public DateTime Date { get; set; }
    }

    #endregion

    #region Com Test Klassen für inherited properties

    internal class MyComDoubleValue : MyComValue
    {
        public double Value { get; set; }
    }

    internal class MyComTextValue : MyComValue
    {
        public string Value { get; set; }
    }

    internal class MyComValue
    {

    }

    internal class MyComObject
    {
        public bool IsEditable { get; set; }
        public MyComValue Value { get; set; }
    }

    #endregion

    #region Test Klassen für enum

    internal enum MyEnumMode
    {
        Normal = 0,
        [XmlEnum("schnell")]
        Fast = 2,
        SuperFast = 4,
    }

    internal class Class1
    {
        public MyEnumMode Mode { get; set; }
    }

    internal class Class2
    {
        public string Mode { get; set; }
    }

    internal class Class3
    {
        public int Mode { get; set; }
    }

    #endregion

    #region Testklassen für Interface map

    internal interface IDateTestRange
    {
        DateTime StartDate { get; }
        DateTime EndDate { get; }

    }

    public interface INotImplementedInterface
    {
        DateTime ADate { get; }

        string AMethod();
    }

    internal class MyDateRange : IDateTestRange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public MyDateRange()
        {
            StartDate = DateTime.Now;
            EndDate = StartDate.AddMonths(12);
        }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }


    #endregion

    #region TestClasses for mapping ListTest

    public class SomeClassWithList
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public SomeClassWithList()
        {
            Elements = new List<string> { "1", "2", "3" };
            Dictionary = new Dictionary<int, string>();
            Dictionary.Add(1, "Flo");
            Dictionary.Add(2, "Tobi");
            Dictionary.Add(3, "Hans");
        }

        public Dictionary<int, string> Dictionary { get; set; }
        public IEnumerable<string> Elements { get; private set; }

        public string[] ElementArray
        {
            get { return Elements.ToArray(); }
        }

        public string Name { get; set; }
    }

    public class SomeOtherClassWithList
    {
        public string Name { get; set; }
        public IEnumerable<int> Elements { get; private set; }
        public int[] ElementArray { get; private set; }
        public IDictionary<string, string> Dictionary { get; set; }

    }

    #endregion

    #region Haupttestklassen

    public class MyInt
    {
        public int Number { get; set; }

        public static implicit operator int(MyInt myInt)
        {
            return myInt.Number;
        }

        public static explicit operator MyInt(int i)
        {
            return new MyInt { Number = i };
        }
    }

    public class MyInfo
    {
        public Adress Adress { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public MyInfo()
        {
            Adress = new Adress { Number = "19", Street = "Am Sonnenhang" };
        }
    }

    public abstract class MyInfoAbstract
    {
        public AbstractAdress Adress { get; set; }
    }


    public class Object1
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public Object1()
        {
            DaInt = 123;
            MyInt = new MyInt { Number = 456 };
            Id = Guid.NewGuid().ToString();
            LevelNull = Guid.Empty;
            Level = Guid.NewGuid();
            privateValue = DateTime.Now.Millisecond;
        }

        public string Id { get; set; }
        public int DaInt;
        public MyInt MyInt;
        public uint ReportId2;
        public int ReportId;
        public string Name;
        public int Age;
        public bool IsMale;
        public Adress Adress;
        public Guid LevelNull;
        public Guid Level;

        private Guid LevelIdValue
        {
            get { return Level; }
        }

        private int privateValue;

        public string GetPrivateValueResult()
        {
            return privateValue.ToString();
        }

        public IEnumerable<Adress> Adresses
        {
            get
            {
                yield return Adress;
                yield return new Adress { Number = "99", Street = "Die STraße" };
            }
        }

        public ExtraShit ExtraShit
        {
            get { return new ExtraShit(); }
        }
    }

    public class Object2
    {
        public Guid Id { get; set; }
        public MyInt DaInt;
        public int MyInt;
        public uint ReportId;
        public int ReportId2;
        public string Name;
        public string Age { get; set; }
        public bool Gender;
        public Adress2 Adress;
        public Guid LevelNull;
        public Guid Level;
        public AbstractAdress[] Adresses;
        public IExtraShit ExtraShit { get; set; }
        public Guid LevelIdValue { get; set; }
        private string privateValue { get; set; }

        public string GetPrivateValueResult()
        {
            return privateValue;
        }
    }

    public interface IExtraShit
    {
        DateTime Time { get; }
        int Number { get; }
        int GetAnInt();
    }

    public abstract class AbstractAdress
    {
        public string Street { get; set; }
        public int Number;
    }


    public class ExtraShit
    {
        public DateTime Time
        {
            get { return DateTime.Now; }
        }

        public int Number
        {
            get { return 42; }
        }
    }

    public class Adress
    {
        public string Street;
        public string Number;
    }

    public class Adress2
    {
        public string Street;
        public int Number;
        public int PostalCode;
    }


    internal class MyCustomObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public MyCustomObject()
        {
            Id = Guid.NewGuid();
            AssignableKey = Guid.NewGuid().ToString();
            NotAssignableKey = "ein string, der definitiv keine GUID ist";
            Number = 42;
        }

        public Guid Id { get; set; }
        public string AssignableKey { get; set; }
        public string NotAssignableKey { get; set; }
        public int Number { get; set; }
        public int AnotherNumber = default(int);
    }

    internal class MyNewCustomObject
    {
        public int Id { get; set; }
        public Guid Number { get; set; }
        public Guid AssignableKey { get; set; }
        public Guid NotAssignableKey { get; set; }
        public MyInt AnotherNumber { get; set; }
    }

    #endregion

    #endregion
}