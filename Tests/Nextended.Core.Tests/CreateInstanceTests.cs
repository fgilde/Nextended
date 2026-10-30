using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nextended.Core.Helper;

namespace Nextended.Core.Tests
{
    public interface ICreateInstanceSample
    {
        string Name { get; set; }
    }

    public class CreateInstancePair
    {
        public CreateInstancePair(Version first, Version second)
        {
            First = first;
            Second = second;
        }

        public Version First { get; }
        public Version Second { get; }
    }

    public class CreateInstanceSelfReference
    {
        public CreateInstanceSelfReference(CreateInstanceSelfReference parent) { }
    }

    [TestClass]
    public class CreateInstanceTests
    {
        // CreateInstance put the interface on the path and then handed it to CreateInstanceFromInterfaceOrAbstractType,
        // which found it there and threw "Cyclic dependency" for every interface
        [TestMethod]
        public void CreatesInstanceForInterfaceWithCycleCheck()
        {
            var instance = ReflectionHelper.CreateInstance<ICreateInstanceSample>(true);

            Assert.IsNotNull(instance);
        }

        [TestMethod]
        public void TwoParametersOfTheSameTypeAreNoCycle()
        {
            var instance = (CreateInstancePair)ReflectionHelper.CreateInstance(typeof(CreateInstancePair), true, false, true);

            Assert.IsNotNull(instance);
        }

        [TestMethod]
        public void RealCycleIsStillDetected()
        {
            Assert.IsNull(ReflectionHelper.CreateInstance(typeof(CreateInstanceSelfReference), true, false, true));
        }
    }
}
