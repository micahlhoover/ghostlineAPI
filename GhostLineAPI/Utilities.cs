using System;

public static class Utilities
{
    public List<object> GetMatchingItems(ValueNameCollection filterKeys, IEnumerable<object> enumerables, Type innerType)
    {
        var returnableItems = new List<object>();

        foreach (var item in enumerables)
        {
            bool allMatched = true;
            foreach (var attributeName in filterKeys.AllKeys)
            {
                var candidateValObject = item.GetType().GetProperty(attributeName).GetValue(item, null);
                var filterKeyVal = filterKeys[attributeName];
                if (!candidateValObject.ToString().Equals(filterKeyVal, StringComparison.InvariantCultureIgnoreCase))
                {
                    allMatched = false;
                }
            }
            if (allMatched)
            {
                returnableItems.Add(item);
            }
        }

        return returnableItems;
    }

}
