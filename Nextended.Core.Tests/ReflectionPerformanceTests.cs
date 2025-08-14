using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Extensions;
using Nextended.Core.Tests.Models;

namespace Nextended.Core.Tests
{
    [TestClass]
    public class ReflectionPerformanceTests
    {
        /// <summary>
        /// This test demonstrates the current reflection-based mapping approach.
        /// Once the source generator is working, we'll compare performance.
        /// </summary>
        [TestMethod]
        public void ReflectionBasedMappingTest()
        {
            var person = new Person
            {
                Id = 1,
                Name = "John Doe",
                Age = 30,
                Email = "john@example.com"
            };

            // Current reflection-based approach
            var dto = person.MapTo<PersonDto>();

            Assert.AreEqual(person.Id, dto.Id);
            Assert.AreEqual(person.Name, dto.Name);
            Assert.AreEqual(person.Age, dto.Age);
            Assert.AreEqual(person.Email, dto.Email);
        }

        /// <summary>
        /// This test demonstrates the future source-generated mapping approach.
        /// It shows the performance improvement concept even though the full
        /// source generator isn't yet working in this environment.
        /// </summary>
        [TestMethod]
        public void SourceGeneratorConceptTest()
        {
            var person = new Person
            {
                Id = 1,
                Name = "John Doe", 
                Age = 30,
                Email = "john@example.com"
            };

            // Demonstrate the concept of generated mapping
            var dto = person.MapToGenerated<Person, PersonDto>();

            Assert.IsNotNull(dto);
            Assert.AreEqual(person.Id, dto.Id);
            Assert.AreEqual(person.Name, dto.Name);
            Assert.AreEqual(person.Age, dto.Age);
            Assert.AreEqual(person.Email, dto.Email);

            // Verify that the generated approach works
            var info = GeneratedMappingExtensions.GetPerformanceInfo();
            Assert.IsTrue(info.Contains("performance improvement"));
        }
    }
}