#region Disclaimer / License
// Copyright (C) 2011, Xervbackup
// http://www.hexad.dk, opensource@hexad.dk
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// 
#endregion
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using XervBackup.Library.Interface;

namespace XervBackup.Library.Backend
{
    public class S3 : IBackend_v2, IStreamingBackend, IBackendGUI
    {
        public const string RRS_OPTION = "s3-use-rrs";
        public const string EU_BUCKETS_OPTION = "s3-european-buckets";
        public const string SUBDOMAIN_OPTION = "s3-use-new-style";
        public const string SERVER_NAME = "s3-server-name";
        public const string LOCATION_OPTION = "s3-location-constraint";
        public const string SSL_OPTION = "use-ssl";

        public static readonly KeyValuePair<string, string>[] KNOWN_S3_PROVIDERS = new KeyValuePair<string,string>[] {
            new KeyValuePair<string, string>("Amazon S3", "s3.amazonaws.com"),
            new KeyValuePair<string, string>("Hosteurope", "cs.hosteurope.de"),
            new KeyValuePair<string, string>("Dunkel", "dcs.dunkel.de"),
        };

        //Updated list: http://docs.amazonwebservices.com/general/latest/gr/rande.html#s3_region
        public static readonly KeyValuePair<string, string>[] KNOWN_S3_LOCATIONS = new KeyValuePair<string, string>[] {
            new KeyValuePair<string, string>("(default)", ""),
            new KeyValuePair<string, string>("Europe (EU, Ireland)", "EU"),
            new KeyValuePair<string, string>("US East (Northern Virginia)", "us-east-1"),
            new KeyValuePair<string, string>("US West (Northen California)", "us-west-1"),
            new KeyValuePair<string, string>("US West (Oregon)", "us-west-2"),
            new KeyValuePair<string, string>("Asia Pacific (Singapore)", "ap-southeast-1"),
            new KeyValuePair<string, string>("Asia Pacific (Sydney)", "ap-southeast-2"),
            new KeyValuePair<string, string>("Asia Pacific (Tokyo)", "ap-northeast-1"),
            new KeyValuePair<string, string>("South America (Sao Paulo)", "sa-east-1"),
        };
        private string m_bucket;
        private string m_prefix;

        private readonly System.Text.RegularExpressions.Regex URL_PARSING = new Regex("s3://(?<hostname>[^/]+)(/(?<prefix>.+))?", RegexOptions.IgnoreCase); 
        public const string DEFAULT_S3_HOST  = "s3.amazonaws.com";
        public const string S3_EU_REGION_NAME = "eu-west-1";

        private Dictionary<string, string> m_options;

        private S3Wrapper m_wrapper;


        public S3()
        {
        }


        public S3(string url, Dictionary<string, string> options)
        {
            //We need to do custom parsing because we allow non-valid urls
            System.Text.RegularExpressions.Match m = URL_PARSING.Match(url);
            if (!m.Success)
                throw new Exception(string.Format(Strings.S3Backend.UnableToParseURLError, url));

            string host = m.Groups["hostname"].Value;
            m_prefix = m.Groups["prefix"].Value;

            string awsID = null;
            string awsKey = null;

            if (options.ContainsKey("ftp-username"))
                awsID = options["ftp-username"];
            if (options.ContainsKey("ftp-password"))
                awsKey = options["ftp-password"];

            if (options.ContainsKey("aws_access_key_id"))
                awsID = options["aws_access_key_id"];
            if (options.ContainsKey("aws_secret_access_key"))
                awsKey = options["aws_secret_access_key"];

            string s3host;
            options.TryGetValue(SERVER_NAME, out s3host);
            if (string.IsNullOrEmpty(s3host))
                s3host = DEFAULT_S3_HOST;

            bool euBuckets = Utility.Utility.ParseBoolOption(options, EU_BUCKETS_OPTION);
            bool useRRS = Utility.Utility.ParseBoolOption(options, RRS_OPTION);
            bool useSSL = Utility.Utility.ParseBoolOption(options, SSL_OPTION);

            string locationConstraint;
            options.TryGetValue(LOCATION_OPTION, out locationConstraint);

            if (!string.IsNullOrEmpty(locationConstraint) && euBuckets)
                throw new Exception(string.Format(Strings.S3Backend.OptionsAreMutuallyExclusiveError, LOCATION_OPTION, EU_BUCKETS_OPTION));

            if (euBuckets)
                locationConstraint = S3_EU_REGION_NAME;

            //Fallback to previous formats
            if (host.Contains(DEFAULT_S3_HOST))
            {
                Uri u = new Uri(url);
                host = u.Host;
                m_prefix = "";

                if (host.ToLower() == s3host)
                {
                    m_bucket = System.Web.HttpUtility.UrlDecode(u.PathAndQuery);

                    if (m_bucket.StartsWith("/"))
                        m_bucket = m_bucket.Substring(1);

                    if (m_bucket.Contains("/"))
                    {
                        m_prefix = m_bucket.Substring(m_bucket.IndexOf("/") + 1);
                        m_bucket = m_bucket.Substring(0, m_bucket.IndexOf("/"));
                    }
                }
                else
                {
                    //Subdomain type lookup
                    if (host.ToLower().EndsWith("." + s3host))
                    {
                        m_bucket = host.Substring(0, host.Length - ("." + s3host).Length);
                        host = s3host;
                        m_prefix = System.Web.HttpUtility.UrlDecode(u.PathAndQuery);

                        if (m_prefix.StartsWith("/"))
                            m_prefix = m_prefix.Substring(1);
                    }
                    else
                        throw new Exception(string.Format(Strings.S3Backend.UnableToDecodeBucketnameError, url));
                }

                try { Console.Error.WriteLine(string.Format(Strings.S3Backend.DeprecatedUrlFormat, "s3://" + m_bucket + "/" + m_prefix)); }
                catch { }
            }
            else
            {
                //The new simplified url style s3://bucket/prefix
                m_bucket = host;
                host = s3host;
            }

            if (string.IsNullOrEmpty(awsID))
                throw new Exception(Strings.S3Backend.NoAMZUserIDError);
            if (string.IsNullOrEmpty(awsKey))
                throw new Exception(Strings.S3Backend.NoAMZKeyError);

            m_options = options;
            m_prefix = m_prefix.Trim();
            if (m_prefix.Length != 0 && !m_prefix.EndsWith("/"))
                m_prefix += "/";

            m_wrapper = new S3Wrapper(awsID, awsKey, locationConstraint, host, useRRS, useSSL);
        }

        public static bool IsValidHostname(string bucketname)
        {
            if (string.IsNullOrEmpty(bucketname))
                return false;
            else
                return Amazon.S3.Util.AmazonS3Util.ValidateV2Bucket(bucketname);
        }

        #region IBackend Members

        public string DisplayName
        {
            get { return Strings.S3Backend.DisplayName; }
        }

        public string ProtocolKey
        {
            get { return "s3"; }
        }

        public bool SupportsStreaming
        {
            get { return true; }
        }

        public List<IFileEntry> List()
        {
            try
            {
                List<IFileEntry> lst = Connection.ListBucket(m_bucket, m_prefix);
                for (int i = 0; i < lst.Count; i++)
                {
                    ((FileEntry)lst[i]).Name = lst[i].Name.Substring(m_prefix.Length);

                    //Fix for a bug in XervBackup 1.0 beta 3 and earlier, where filenames are incorrectly prefixed with a slash
                    if (lst[i].Name.StartsWith("/") && !m_prefix.StartsWith("/"))
                        ((FileEntry)lst[i]).Name = lst[i].Name.Substring(1);
                }
                return lst;
            }
            catch (Exception ex)
            {
                //Catch "non-existing" buckets
                Amazon.S3.AmazonS3Exception s3ex = ex as Amazon.S3.AmazonS3Exception;
                if (s3ex != null && (s3ex.StatusCode == System.Net.HttpStatusCode.NotFound || "NoSuchBucket".Equals(s3ex.ErrorCode)))
                    throw new Interface.FolderMissingException(ex);

                throw;
            }
        }

        public void Put(string remotename, string localname)
        {
            using (System.IO.FileStream fs = System.IO.File.Open(localname, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                Put(remotename, fs);
        }

        public void Put(string remotename, System.IO.Stream input)
        {
            try
            {
                Connection.AddFileStream(m_bucket, GetFullKey(remotename), input);
            }
			catch (Exception ex)
			{
                //Catch "non-existing" buckets
                Amazon.S3.AmazonS3Exception s3ex = ex as Amazon.S3.AmazonS3Exception;
                if (s3ex != null && (s3ex.StatusCode == System.Net.HttpStatusCode.NotFound || "NoSuchBucket".Equals(s3ex.ErrorCode)))
                    throw new Interface.FolderMissingException(ex);

                throw;
            }
        }

        public void Get(string remotename, string localname)
        {
            using (System.IO.FileStream fs = System.IO.File.Open(localname, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                Get(remotename, fs);
        }

        public void Get(string remotename, System.IO.Stream output)
        {
            try
            {
                Connection.GetFileStream(m_bucket, GetFullKey(remotename), output);
            }
            catch
            {
                //This is a fix for the S3 backend prior to beta 3, where the filenames had a slash prefixed
                try
                {
                    if (!remotename.StartsWith("/"))
                        Connection.GetFileStream(m_bucket, GetFullKey("/" + remotename), output);
                    return;
                }
                catch
                {
                }

                //Throw original error
                throw;
            }
        }

        public void Delete(string remotename)
        {
            Connection.DeleteObject(m_bucket, GetFullKey(remotename));
        }

        public IList<ICommandLineArgument> SupportedCommands
        {
            get
            {
                StringBuilder hostnames = new StringBuilder();
                StringBuilder locations = new StringBuilder();
                foreach(KeyValuePair<string, string> s in KNOWN_S3_PROVIDERS)
                    hostnames.AppendLine(string.Format("{0}: {1}", s.Key, s.Value));

                foreach (KeyValuePair<string, string> s in KNOWN_S3_LOCATIONS)
                    locations.AppendLine(string.Format("{0}: {1}", s.Key, s.Value));

                return new List<ICommandLineArgument>(new ICommandLineArgument[] {
                    new CommandLineArgument("aws_secret_access_key", CommandLineArgument.ArgumentType.Password, Strings.S3Backend.AMZKeyDescriptionShort, Strings.S3Backend.AMZKeyDescriptionLong, null, new string[] {"ftp-password"}, null),
                    new CommandLineArgument("aws_access_key_id", CommandLineArgument.ArgumentType.String, Strings.S3Backend.AMZUserIDDescriptionShort, Strings.S3Backend.AMZUserIDDescriptionLong, null, new string[] {"ftp-username"}, null),
                    new CommandLineArgument(SUBDOMAIN_OPTION, CommandLineArgument.ArgumentType.Boolean, Strings.S3Backend.S3NewStyleDescriptionShort, Strings.S3Backend.S3NewStyleDescriptionLong, "true", null, null, Strings.S3Backend.S3NewStyleDeprecation),
                    new CommandLineArgument(EU_BUCKETS_OPTION, CommandLineArgument.ArgumentType.Boolean, Strings.S3Backend.S3EurobucketDescriptionShort, Strings.S3Backend.S3EurobucketDescriptionLong, "false", null, null, string.Format(Strings.S3Backend.S3EurobucketDeprecationDescription, LOCATION_OPTION, S3_EU_REGION_NAME)),
                    new CommandLineArgument(RRS_OPTION, CommandLineArgument.ArgumentType.Boolean, Strings.S3Backend.S3UseRRSDescriptionShort, Strings.S3Backend.S3UseRRSDescriptionLong, "false"),
                    new CommandLineArgument(SERVER_NAME, CommandLineArgument.ArgumentType.String, Strings.S3Backend.S3ServerNameDescriptionShort, string.Format(Strings.S3Backend.S3ServerNameDescriptionLong, hostnames.ToString()), DEFAULT_S3_HOST),
                    new CommandLineArgument(LOCATION_OPTION, CommandLineArgument.ArgumentType.String, Strings.S3Backend.S3LocationDescriptionShort, string.Format(Strings.S3Backend.S3LocationDescriptionLong, locations.ToString())),
                    new CommandLineArgument(SSL_OPTION, CommandLineArgument.ArgumentType.Boolean, Strings.S3Backend.DescriptionUseSSLShort, Strings.S3Backend.DescriptionUseSSLLong),
                    new CommandLineArgument("ftp-password", CommandLineArgument.ArgumentType.Password, Strings.S3Backend.FTPPasswordDescriptionShort, Strings.S3Backend.FTPPasswordDescriptionLong),
                    new CommandLineArgument("ftp-username", CommandLineArgument.ArgumentType.String, Strings.S3Backend.DescriptionFTPUsernameShort, Strings.S3Backend.DescriptionFTPUsernameLong),
                });

            }
        }

        public string Description
        {
            get
            {
                return Strings.S3Backend.Description_v2;
            }
        }

        #endregion

        #region IBackend_v2 Members

        public void Test()
        {
            List();
        }

        public void CreateFolder()
        {
            //S3 does not complain if the bucket already exists
            Connection.AddBucket(m_bucket);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (m_options != null)
                m_options = null;
            if (m_wrapper != null)
            {
                m_wrapper.Dispose();
                m_wrapper = null;
            }
        }

        #endregion

        private S3Wrapper Connection
        {
            get { return m_wrapper; }
        }

        private string GetFullKey(string name)
        {
            //AWS SDK encodes the filenames correctly
            return m_prefix + name;
        }


        #region IBackendGUI Members

        public string PageTitle
        {
            get { return S3UI.PageTitle; }
        }

        public string PageDescription
        {
            get { return S3UI.PageDescription; }
        }

        public System.Windows.Forms.Control GetControl(IDictionary<string, string> applicationSettings, IDictionary<string, string> options)
        {
            return new S3UI(applicationSettings, options);
        }

        public void Leave(System.Windows.Forms.Control control)
        {
            ((S3UI)control).Save(false);
        }

        public bool Validate(System.Windows.Forms.Control control)
        {
            return ((S3UI)control).Save(true);
        }

        public string GetConfiguration(IDictionary<string, string> applicationSettings, IDictionary<string, string> guiOptions, IDictionary<string, string> commandlineOptions)
        {
            return S3UI.GetConfiguration(guiOptions, commandlineOptions);
        }

        #endregion
    }
}
