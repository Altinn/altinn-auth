namespace Altinn.ResourceRegistry.Core.Extensions
{
    /// <summary>
    /// Generic class to convert a single entity to a List containing the entity
    /// </summary>
    public static class ToListExtension
    {
        /// <summary>
        /// Generic method to get a List from a single instance 
        /// </summary>
        /// <typeparam name="T">Type of the input</typeparam>
        /// <param name="input">the input data</param>
        /// <returns>List containing the instance</returns>
        public static List<T> ElementToList<T>(this T input)
        {
            return new List<T> { input };
        }
    }
}
