﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Runtime.Serialization.Json;
using poshsecframework.Strings;

namespace poshsecframework.Web
{
    /// <summary>
    /// A WebRequest wrapper class for the Github API.
    /// </summary>
    class GithubClient
    {
        #region Private Variables
        HttpWebRequest ghc = null;
        List<String> errors = new List<string>();
        int ratelimitremaining = 0;
        #endregion

        #region Public Methods
        /// <summary>
        /// Requests the JSON response from the Github API for a list of available branches for the given repository.
        /// </summary>
        /// <param name="OwnerName">The owner of the repository.</param>
        /// <param name="RepositoryName">The name of the repository.</param>
        /// <returns></returns>
        public Collection<GithubJsonItem> GetBranches(String OwnerName, String RepositoryName)
        {
            Collection<GithubJsonItem> ghi = Get(Path.Combine(StringValue.GithubURI, String.Format(StringValue.BranchFormat, OwnerName, RepositoryName)));
            return ghi;
        }

        /// <summary>
        /// Downloads the zipball of the specified repository and branch and unzips it to the Module Directory.
        /// </summary>
        /// <param name="OwnerName">The owner of the repository.</param>
        /// <param name="RepositoryName">The name of the repository.</param>
        /// <param name="BranchItem">The selected branch item.</param>
        /// <param name="ModuleDirectory">The target directory for the zipball to be extracted into.</param>
        public void GetArchive(String OwnerName, String RepositoryName, GithubJsonItem BranchItem, String ModuleDirectory)
        {
            String tmpfile = Path.GetTempFileName();
            FileInfo savedfile = Download(Path.Combine(StringValue.GithubURI, String.Format(StringValue.ArchiveFormat, OwnerName, RepositoryName, BranchItem.Name)), tmpfile);
            if (savedfile != null)
            {
                try
                {
                    String target = Path.Combine(ModuleDirectory, RepositoryName);
                    System.IO.Compression.ZipArchive za = System.IO.Compression.ZipFile.Open(savedfile.FullName, System.IO.Compression.ZipArchiveMode.Read);
                    if (za != null && za.Entries.Count() > 0)
                    {
                        using (za)
                        {
                            if (IsValidPSModule(za))
                            {
                                String parentfolder = za.Entries[0].FullName;
                                System.IO.Compression.ZipFile.ExtractToDirectory(savedfile.FullName, ModuleDirectory);
                                String newfolder = Path.Combine(ModuleDirectory, parentfolder);
                                if (Directory.Exists(newfolder))
                                {
                                    DirectoryInfo di = new DirectoryInfo(newfolder);
                                    if (Directory.Exists(target))
                                    {
                                        Directory.Delete(target, true);
                                    }
                                    di.MoveTo(target);
                                    di = null;
                                }
                            }
                            else
                            {
                                errors.Add(String.Format(StringValue.InvalidPSModule, RepositoryName, BranchItem.Name));
                            }
                        }
                    }
                    za = null;
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                }
            }
            try
            {
                File.Delete(savedfile.FullName);
            }
            catch (Exception dex)
            {
                errors.Add(dex.Message);

            }
            savedfile = null;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Checks to see if the downloaded zipball contains a psd1 file in the root folder. This is required for the powershell modules.
        /// </summary>
        /// <param name="zaitem">The ZipArchive item of the zipball.</param>
        /// <returns>True if the zipball contains a psd1 file. Default is false.</returns>
        private bool IsValidPSModule(System.IO.Compression.ZipArchive zaitem)
        {
            bool rtn = false;
            int idx = 0;
            if (zaitem != null)
            {
                string rootfolder = zaitem.Entries[0].FullName;
                do
                {
                    System.IO.Compression.ZipArchiveEntry zaentry = zaitem.Entries[idx];
                    string filename = zaentry.FullName.Replace(rootfolder, "");
                    if (!filename.Contains("/") && filename.Contains(".psd1"))
                    {
                        rtn = true;
                    }                    
                    idx++;
                } while (!rtn && idx < zaitem.Entries.Count());
            }            
            return rtn;
        }


        private string GetLastCommit(GithubJsonItem BranchItem)
        {
            string rtn = "";
            Collection<GithubJsonItem> brnchinfo = Get(BranchItem.URL);
            if (brnchinfo != null && brnchinfo.Count() > 0)
            { 
                
            }
            return rtn;
        }

        /// <summary>
        /// Downloads the specific file from the Github API.
        /// </summary>
        /// <param name="uri">The url of the file to download.</param>
        /// <param name="targetfile">The local filename to use.</param>
        /// <returns></returns>
        private FileInfo Download(String uri, String targetfile)
        {
            FileInfo rtn = null;
            ghc = (HttpWebRequest)WebRequest.Create(uri);
            ghc.UserAgent = StringValue.psftitle;
            WebResponse ghr = null;
            try
            {
                ghr = ghc.GetResponse();
            }
            catch (Exception e)
            {
                errors.Add(uri + ":" + e.Message);
            }
            if (ghr != null)
            {
                try
                {
                    Stream ghrs = ghr.GetResponseStream();
                    int pos = 0;
                    byte[] bytes = new byte[(ghr.ContentLength)];
                    while (pos < bytes.Length)
                    {
                        int bytread = ghrs.Read(bytes, pos, bytes.Length - pos);
                        pos += bytread;
                        //UpdateProgressHere
                    }
                    ghrs.Close();

                    Stream str = new FileStream(targetfile, FileMode.Create);
                    BinaryWriter wtr = new BinaryWriter(str);
                    wtr.Write(bytes);
                    wtr.Flush();
                    wtr.Close();
                    str.Close();
                    wtr = null;
                    str = null;
                    rtn = new FileInfo(targetfile);
                }
                catch (Exception wex)
                {
                    errors.Add(uri + ":" + wex.Message);
                }
            }
            return rtn;
        }

        /// <summary>
        /// Performs a Get WebRequest to the specified URL and returns a collection of GithubJsonItems.
        /// </summary>
        /// <param name="uri">The url for the Get Request.</param>
        /// <returns></returns>
        private Collection<GithubJsonItem> Get(String uri)
        {
            Collection<GithubJsonItem> rtn = new Collection<GithubJsonItem>();
            ghc = (HttpWebRequest)WebRequest.Create(uri);
            ghc.UserAgent = StringValue.psftitle;
            WebResponse ghr = null;
            try
            {
                ghr = ghc.GetResponse();
            }
            catch (WebException wex)
            {
                ratelimitremaining = GetRateLimitRemaining(wex.Response);
                errors.Add(uri + ":" + wex.Message);
            }
            catch (Exception e)
            {
                errors.Add(uri + ":" + e.Message);
            }
            if (ghr != null)
            {
                ratelimitremaining = GetRateLimitRemaining(ghr);
                if (ghr.ContentType == StringValue.ContentTypeJSON)
                {
                    Stream ghrs = ghr.GetResponseStream();
                    if (ghrs != null)
                    {
                        StreamReader ghrdr = new StreamReader(ghrs);
                        String response = ghrdr.ReadToEnd();
                        ghrdr.Close();
                        ghrdr = null;
                        String name = "\"name\"";
                        String[] split = new string[] { "\"name\"" };
                        String[] items = response.Split(split, StringSplitOptions.None);
                        if (items != null && items.Count() > 0)
                        {
                            foreach (String resp in items)
                            {
                                if (resp != "[{" && resp != "{")
                                {
                                    GithubJsonItem gjson = new GithubJsonItem(name + resp);
                                    rtn.Add(gjson);
                                }
                            }
                        }
                        ghrs.Close();
                        ghrs = null;
                    }
                }
                ghr.Close();
                ghr = null;
            }
            return rtn;
        }

        private int GetRateLimitRemaining(WebResponse ghr)
        {
            int rtn = 0;
            if (ghr.Headers.Keys.Count > 0)
            {
                int idx = -1;
                bool found = false;
                do
                {
                    idx++;
                    if (ghr.Headers.Keys[idx] == StringValue.RateLimitKey)
                    {
                        found = true;
                    }
                } while (!found && idx < ghr.Headers.Keys.Count);
                if (found)
                {
                    string[] vals = ghr.Headers.GetValues(idx);
                    if (vals != null)
                    {
                        string val = vals[0];
                        int.TryParse(val, out rtn);
                    }                    
                }
            }
            return rtn;
        }

        /// <summary>
        /// Decodes the base64 encoded content.
        /// </summary>
        /// <param name="encodedstring">The base64 encoded content.</param>
        /// <returns></returns>
        private byte[] Decode(String encodedstring)
        {
            byte[] rtn = null;
            try
            {
                UTF8Encoding enc = new UTF8Encoding();
                Decoder dec = enc.GetDecoder();
                rtn = Convert.FromBase64String(encodedstring.Replace("\\n", ""));                
            }
            catch (Exception e)
            {
                errors.Add("Decode failed: " + e.Message);
                rtn = null;
            }
            return rtn;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// List of any errors that may have occured during any request.
        /// </summary>
        public List<String> Errors
        {
            get { return errors; }
        }

        public int RateLimitRemaining
        {
            get { return ratelimitremaining; }
        }
        #endregion        
    }
}