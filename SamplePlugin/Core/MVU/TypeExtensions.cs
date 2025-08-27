using System;
using System.Linq;

namespace SamplePlugin.Core.MVU;

public static class TypeExtensions
{
    public static bool IsRecord(this Type type)
    {
        try
        {
            // Records are reference types (classes), not value types
            if (type.IsValueType)
                return false;
            
            // Check if the type has EqualityContract property (records have this)
            var equalityContractProperty = type.GetProperty("EqualityContract", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
                
            // Check if the type has <Clone>$ method (another record indicator)
            var cloneMethod = type.GetMethod("<Clone>$", 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance);
            
            // Check for a copy constructor (parameter of the same type)
            var hasCopyConstructor = type.GetConstructors()
                .Any(c => c.GetParameters().Length == 1 && 
                         c.GetParameters()[0].ParameterType == type);
            
            // Consider it a record if it has at least two of these indicators
            // This reduces false positives from classes that might coincidentally have one
            var indicators = new[] 
            { 
                equalityContractProperty != null,
                cloneMethod != null,
                hasCopyConstructor
            }.Count(x => x);
            
            return indicators >= 2;
        }
        catch
        {
            // If reflection fails for any reason, assume it's not a record
            // This ensures the fallback mechanism in the calling code is used
            return false;
        }
    }
}
