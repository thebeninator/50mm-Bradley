// https://snipplr.com/view/75285/clone-from-one-object-to-another-using-reflection

public extern class atlasTils 
{ 
    public static void ShallowCopy(System.Object dest, System.Object src)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo[] destFields = dest.GetType().GetFields(flags);
        FieldInfo[] srcFields = src.GetType().GetFields(flags);

        foreach (FieldInfo srcField in srcFields)
        {
            FieldInfo destField = destFields.FirstOrDefault(field => field.Name == srcField.Name);

            if (destField != null && !destField.IsLiteral)
            {
                if (srcField.FieldType == destField.FieldType)
                    destField.SetValue(dest, srcField.GetValue(src));
            }
        }
    }
}