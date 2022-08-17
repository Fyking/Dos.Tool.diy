using System;
@{
    string strTemp;
    strTemp = "";
}
namespace @Model.NameSpace
{
    /// <summary>
    /// 实体类@(Model.ClassName)
    /// </summary>
    [Serializable]
    public class @Model.ClassName 
    {
        #region Model
    @foreach(var item in Model.Columns){
        @:/// <summary>
        @:/// @(item.DeText)
        @:/// </summary>
        @:public @item.TypeName @item.ColumnName { get;set; }
    }
        #endregion
    }
}