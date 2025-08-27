using System;

namespace SamplePlugin.Core.MVU;

public static class TypeExtensions
{
    public static bool IsRecord(this Type type)
    {
        // Check if type has EqualityContract property (records have this)
        var equalityContractProperty = type.GetProperty("EqualityContract", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
            
        if (equalityContractProperty != null)
            return true;
            
        // Check if type has <Clone>$ method (another record indicator)
        var cloneMethod = type.GetMethod("<Clone>$", 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance);
            
        return cloneMethod != null;
    }
}
