using System;
using System.ComponentModel;
using System.Data;

namespace DGCore.UserSettings
{
    public interface IUserSettingProperties
    {
        string SettingKind { get; }
        string SettingKey { get; }
    }

    public interface IUserSettingSupport<T> : IUserSettingProperties
    {
        T GetSettings();
        T GetBlankSetting();
        void SetSetting(T settings);
    }

    public interface ISupportSerializationModifications
    {
        void ModifyBeforeSerialize();
        void ModifyAfterDeserialized();
    }

    public class FakeUserSettingProperties : IUserSettingProperties
    {
        public string SettingKind { get; set; }
        public string SettingKey { get; set; }
    }

    public class UserSettingsDbObject : INotifyPropertyChanged
    {
        internal bool OriginalAllowViewOthers;
        internal bool OriginalAllowEditOthers;

        public string SettingId { get; }
        public bool AllowViewOthers { get; set; }
        public bool AllowEditOthers { get; set; }
        public string Created { get; }
        public DateTime DCreated { get; }
        public string Updated { get; }
        public DateTime? DUpdated { get; }
        public bool IsDeleted { get; set; } = false;

        public UserSettingsDbObject(IDataRecord dr)
        {
            SettingId = dr["id"].ToString();
            AllowViewOthers = (bool)dr["allowViewOthers"];
            AllowEditOthers = (bool)dr["allowEditOthers"];
            Created = dr["created"].ToString().Trim().ToUpper();
            DCreated = (DateTime)dr["dcreated"];

            var o = dr["updated"];
            if (o != DBNull.Value)
                Updated = ((string)o).Trim().ToUpper();

            o = dr["dupdated"];
            if (o != DBNull.Value)
                DUpdated = (DateTime)o;

            OriginalAllowViewOthers = AllowViewOthers;
            OriginalAllowEditOthers = AllowEditOthers;
        }

        public bool IsEditable => OriginalAllowEditOthers || Created == Utils.Tips.GetFullUserName();
        public bool IsChanged => (AllowEditOthers != OriginalAllowEditOthers) || (AllowViewOthers != OriginalAllowViewOthers);

        #region ============  INotifyPropertyChanged  ============
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}

