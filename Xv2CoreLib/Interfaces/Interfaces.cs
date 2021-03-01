﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xv2CoreLib
{
    /// <summary>
    /// Provides a function for detecting if a file has any meaningful data in it.
    /// </summary>
    public interface IIsNull
    {
        bool IsNull();
    }

    /// <summary>
    /// Provides a unique ID that can be used for installation/uninstallation purposes. This can be an int, string or multiple ints merged together. 
    /// </summary>
    public interface IInstallable
    {
        int SortID { get; }
        string Index { get; set; }
    }

    /// <summary>
    /// Alternative to IInstallable just for sorting.
    /// </summary>
    public interface ISortable
    {
        int SortID { get; }
    }

    public interface IInstallable_2<T> where T : IInstallable
    {
        List<T> SubEntries { get; set; }
    }

    public interface ISorting
    {
        void SortEntries();
    }
    
    /// <summary>
    /// Enables the AutoID binding function for this property. Note: Only one AutoID binding is allowed per section, and it MUST be on Index value, which implements IInstallable.
    /// </summary>
    public class BindingAutoId : Attribute
    {
        public ushort MaxId { get; set; }

        public BindingAutoId(ushort maxId = ushort.MaxValue)
        {
            MaxId = maxId;
        }
    }

    /// <summary>
    /// Enables values to be binded on a sub class.
    /// </summary>
    public class BindingSubClass : Attribute
    {
    }

    /// <summary>
    /// Enables values to be binded on a IList collection of objects.
    /// </summary>
    public class BindingSubList : Attribute
    {

    }

    /// <summary>
    /// Marks this property as a string value type, allowing non-numeric characters and multiple bindings within.\n
    /// Any string not marked with this will be treated as an int by the binding manager.
    /// </summary>
    public class BindingString : Attribute
    {

    }

}
