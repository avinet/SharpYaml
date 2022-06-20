﻿// Copyright (c) 2015 SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using NUnit.Framework;
using SharpYaml.Serialization;
using SharpYaml.Serialization.Descriptors;

namespace SharpYaml.Tests
{
    public class DescriptorTests
    {
        public class TestObject
        {
            // unused, not public
            internal string InternalName { get; set; }

            public TestObject()
            {
                Collection = new List<string>();
                CollectionReadOnly = new ReadOnlyCollection<string>(new List<string>());
                DefaultValue = 5;
            }

            public object Value { get; set; }

            public string Name;

            public string Property { get; set; }

            public ICollection<string> Collection { get; set; }

            public ICollection<string> CollectionReadOnly { get; private set; }

            [YamlIgnore]
            public string DontSerialize { get; set; }

            [YamlMember("Item1")]
            public string ItemRenamed1 { get; set; }

            // This property is renamed to Item2 by an external attribute
            public int ItemRenamed2 { get; set; }

            [DefaultValue(5)]
            public int DefaultValue { get; set; }

            public bool ShouldSerializeValue()
            {
                return Value != null;
            }
        }

        [Test]
        public void TestObjectDescriptor()
        {
            var attributeRegistry = new AttributeRegistry();

            // Rename ItemRenamed2 to Item2
            attributeRegistry.Register(typeof(TestObject).GetProperty("ItemRenamed2"), new YamlMemberAttribute("Item2"));

            var descriptor = new ObjectDescriptor(attributeRegistry, typeof(TestObject), false, false, new DefaultNamingConvention());
            descriptor.Initialize();

            // Verify members
            Assert.AreEqual(8, descriptor.Count);

            descriptor.SortMembers(new DefaultKeyComparer());

            // Check names and their orders
            Assert.AreEqual(descriptor.Members.Select(memberDescriptor => memberDescriptor.Name), new[]
            {
                "Collection",
                "CollectionReadOnly",
                "DefaultValue",
                "Item1",
                "Item2",
                "Name",
                "Property",
                "Value"
            });

            var instance = new TestObject { Name = "Yes", Property = "property" };

            // Check field accessor
            Assert.AreEqual("Yes", descriptor["Name"].Get(instance));
            descriptor["Name"].Set(instance, "No");
            Assert.AreEqual("No", instance.Name);

            // Check property accessor
            Assert.AreEqual("property", descriptor["Property"].Get(instance));
            descriptor["Property"].Set(instance, "property1");
            Assert.AreEqual("property1", instance.Property);

            // Check ShouldSerialize
            Assert.True(descriptor["Name"].ShouldSerialize(instance));

            Assert.False(descriptor["Value"].ShouldSerialize(instance));
            instance.Value = 1;
            Assert.True(descriptor["Value"].ShouldSerialize(instance));

            Assert.False(descriptor["DefaultValue"].ShouldSerialize(instance));
            instance.DefaultValue++;
            Assert.True(descriptor["DefaultValue"].ShouldSerialize(instance));

            // Check HasSet
            Assert.True(descriptor["Collection"].HasSet);
            Assert.False(descriptor["CollectionReadOnly"].HasSet);
        }

        public class TestObjectNamingConvention
        {
            public string Name { get; set; }

            public string ThisIsCamelName { get; set; }

            [YamlMember("myname")]
            public string CustomName { get; set; }
        }

        [Test]
        public void TestObjectWithCustomNamingConvention()
        {
            var attributeRegistry = new AttributeRegistry();
            var descriptor = new ObjectDescriptor(attributeRegistry, typeof(TestObjectNamingConvention), false, false, new FlatNamingConvention());
            descriptor.Initialize();

            descriptor.SortMembers(new DefaultKeyComparer());

            // Check names and their orders
            Assert.AreEqual(descriptor.Members.Select(memberDescriptor => memberDescriptor.Name), new[]
            {
                "myname",
                "name",
                "this_is_camel_name"
            });
        }

        /// <summary>
        /// This is a non pure collection: It has at least one public get/set member.
        /// </summary>
        public class NonPureCollection : List<int>
        {
            public string Name { get; set; }
        }

        [Test]
        public void TestCollectionDescriptor()
        {
            var attributeRegistry = new AttributeRegistry();
            var descriptor = new CollectionDescriptor(attributeRegistry, typeof(List<string>), false, false, new DefaultNamingConvention());
            descriptor.Initialize();

            // No Capacity as a member
            Assert.AreEqual(0, descriptor.Count);
            Assert.True(descriptor.IsPureCollection);
            Assert.AreEqual(typeof(string), descriptor.ElementType);

            descriptor = new CollectionDescriptor(attributeRegistry, typeof(NonPureCollection), false, false,
                new DefaultNamingConvention());
            descriptor.Initialize();

            // Has name as a member
            Assert.AreEqual(1, descriptor.Count);
            Assert.False(descriptor.IsPureCollection);
            Assert.AreEqual(typeof(int), descriptor.ElementType);

            descriptor = new CollectionDescriptor(attributeRegistry, typeof(ArrayList), false, false, new DefaultNamingConvention());
            descriptor.Initialize();

            // No Capacity
            Assert.AreEqual(0, descriptor.Count);
            Assert.True(descriptor.IsPureCollection);
            Assert.AreEqual(typeof(object), descriptor.ElementType);
        }

        /// <summary>
        /// This is a non pure collection: It has at least one public get/set member.
        /// </summary>
        public class NonPureDictionary : Dictionary<float, object>
        {
            public string Name { get; set; }
        }

        [Test]
        public void TestDictionaryDescriptor()
        {
            var attributeRegistry = new AttributeRegistry();
            var descriptor = new DictionaryDescriptor(attributeRegistry, typeof(Dictionary<int, string>), false, false,
                new DefaultNamingConvention());
            descriptor.Initialize();

            Assert.AreEqual(0, descriptor.Count);
            Assert.True(descriptor.IsPureDictionary);
            Assert.AreEqual(typeof(int), descriptor.KeyType);
            Assert.AreEqual(typeof(string), descriptor.ValueType);

            descriptor = new DictionaryDescriptor(attributeRegistry, typeof(NonPureDictionary), false, false,
                new DefaultNamingConvention());
            descriptor.Initialize();
            Assert.AreEqual(1, descriptor.Count);
            Assert.False(descriptor.IsPureDictionary);
            Assert.AreEqual(typeof(float), descriptor.KeyType);
            Assert.AreEqual(typeof(object), descriptor.ValueType);
        }

        [Test]
        public void TestArrayDescriptor()
        {
            var attributeRegistry = new AttributeRegistry();
            var descriptor = new ArrayDescriptor(attributeRegistry, typeof(int[]), new DefaultNamingConvention());
            descriptor.Initialize();

            Assert.AreEqual(0, descriptor.Count);
            Assert.AreEqual(typeof(int), descriptor.ElementType);
        }

        public enum MyEnum
        {
            A,
            B
        }

        [Test]
        public void TestPrimitiveDescriptor()
        {
            var attributeRegistry = new AttributeRegistry();
            var descriptor = new PrimitiveDescriptor(attributeRegistry, typeof(int), new DefaultNamingConvention());
            Assert.AreEqual(0, descriptor.Count);

            Assert.True(PrimitiveDescriptor.IsPrimitive(typeof(MyEnum)));
            Assert.True(PrimitiveDescriptor.IsPrimitive(typeof(object)));
            Assert.True(PrimitiveDescriptor.IsPrimitive(typeof(DateTime)));
            Assert.True(PrimitiveDescriptor.IsPrimitive(typeof(TimeSpan)));
            Assert.False(PrimitiveDescriptor.IsPrimitive(typeof(IList)));
        }
    }
}
