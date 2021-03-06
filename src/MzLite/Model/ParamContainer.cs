﻿using System;
using System.ComponentModel;
using MzLite.Json;
using Newtonsoft.Json;

namespace MzLite.Model
{

    public abstract class ParamBase : INotifyPropertyChanged, INotifyPropertyChanging
    {
        
        private string cvUnitAccession;        
        private IConvertible value;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string CvUnitAccession 
        {
            get { return cvUnitAccession; }
            set
            {
                if (cvUnitAccession != value)
                {
                    NotifyPropertyChanging("CvUnitAccession");
                    this.cvUnitAccession = value;
                    NotifyPropertyChanged("CvUnitAccession");
                }
            }
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ConvertibleConverter))]
        public IConvertible Value
        {
            get { return value; }
            set
            {
                if (this.value != value)
                {
                    NotifyPropertyChanging("Value");
                    this.value = value;
                    NotifyPropertyChanged("Value");
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            if ((this.PropertyChanged != null))
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;

        protected void NotifyPropertyChanging(string propertyName)
        {
            if ((this.PropertyChanging != null))
            {
                this.PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class CvParam : ParamBase
    {

        private readonly string cvAccession;

        [JsonConstructor]
        public CvParam([JsonProperty("CvAccession")] string cvAccession)
        {
            if (string.IsNullOrWhiteSpace(cvAccession))
                throw new ArgumentNullException("cvAccession");

            this.cvAccession = cvAccession;
        }

        [JsonProperty(Required = Required.Always)]
        public string CvAccession { get { return cvAccession; } }

        public override string ToString()
        {
            if (CvUnitAccession == null)
                return string.Format("'{0}','{1}'",
                    CvAccession,
                    Value == null ? "null" : Value.ToString());
            else
                return string.Format("'{0}','{1}','{2}'",
                    CvAccession,
                    Value == null ? "null" : Value.ToString(),
                    CvUnitAccession);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class UserParam : ParamBase
    {
        
        private readonly string name;
        
        [JsonConstructor]
        public UserParam([JsonProperty("Name")] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");
            this.name = name;
        }

        #region INamedItem Members

        [JsonProperty(Required = Required.Always)]
        public string Name
        {
            get
            {
                return name;
            }            
        }

        #endregion

        public override string ToString()
        {
            if (CvUnitAccession == null)
                return string.Format("'{0}','{1}'",
                    Name,
                    Value == null ? "null" : Value.ToString());
            else
                return string.Format("'{0}','{1}','{2}'",
                    Name,
                    Value == null ? "null" : Value.ToString(),
                    CvUnitAccession);
        }
    }

    public interface IParamContainer
    {
        CvParamCollection CvParams { get; }
        UserParamCollection UserParams { get; }
        UserDescriptionList UserDescriptions { get; }
    }

    public abstract class ParamContainer : IParamContainer
    {

        protected ParamContainer()
        {
            cvParams = new CvParamCollection();
            userParams = new UserParamCollection();
            userDescriptions = new UserDescriptionList();
        }

        private readonly CvParamCollection cvParams;
        private readonly UserParamCollection userParams;
        private readonly UserDescriptionList userDescriptions;

        [JsonProperty]
        public CvParamCollection CvParams { get { return cvParams; } }

        [JsonProperty]
        public UserParamCollection UserParams { get { return userParams; } }

        [JsonProperty]
        public UserDescriptionList UserDescriptions { get { return userDescriptions; } }
    }

    [JsonArray]
    public sealed class CvParamCollection : ObservableKeyedCollection<string, CvParam>
    {
        [JsonConstructor]
        internal CvParamCollection() : base(StringComparer.InvariantCultureIgnoreCase) { }

        protected override string GetKeyForItem(CvParam item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            return item.CvAccession;
        }        
    }

    [JsonArray]
    public sealed class UserParamCollection : ObservableKeyedCollection<string, UserParam>
    {
        [JsonConstructor]
        internal UserParamCollection() : base() { }

        protected override string GetKeyForItem(UserParam item)
        {
            if (item == null)
                throw new ArgumentNullException("item");           
            return item.Name;
        }       
    }    
}
