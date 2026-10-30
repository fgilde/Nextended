using System;
using System.Collections.Generic;
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

    public interface IGrowing<T> { }

    // every level needs the next larger generic type, so no type repeats and the cycle check never fires
    public class Growing<T> : IGrowing<T>
    {
        public Growing(IGrowing<List<T>> next) { }
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

        // An implementation whose constructor needs an ever larger generic type overflowed the stack (it took down a
        // trimmed Blazor WebAssembly app); the creation path is limited in depth now
        [TestMethod]
        public void EverGrowingGenericConstructorChainEnds()
        {
            ReflectionHelper.CreateInstance(typeof(IGrowing<int>), true, false, true);
            ReflectionHelper.CreateInstance(typeof(Growing<int>), true, false, false);
        }
    }
}
