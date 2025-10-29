using System;
using System.Collections.Generic;

namespace XTension_template
{
    public class EvidenceObject
    {
        public IntPtr _hVolume { get; set; }
        public string Name  { get; set; }
        public List<User> Users = new List<User>();

        public EvidenceObject(IntPtr _hVolume, string name)
        {
            this._hVolume = _hVolume;
            this.Name = name;
        }
        public EvidenceObject(IntPtr _hVolume, string name, User user)
        {
            this._hVolume = _hVolume;
            this.Name = name;
            this.Users.Add(user);
        }
    }

    public class User
    {
        public string Name { get; set; }
        public List<Profile> Profiles = new List<Profile>();

        public User(string name)
        {
            this.Name = name;
        }

        public User(string name, Profile profile)
        {
            this.Name = name;
            this.Profiles.Add(profile);
        }
    }

    public class Profile
    {
        public string Name { get; set; }
        public string DataPath { get; set; }

        public Profile(string name, string dataPath)
        {
            this.Name = name;
            this.DataPath = dataPath;
        }
    }
}
