﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Kalus.Updater.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Kalus.Updater.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Deleting temporary files....
        /// </summary>
        internal static string DeleteTempFile {
            get {
                return ResourceManager.GetString("DeleteTempFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There was an error downloading the new version :.
        /// </summary>
        internal static string DownloadError {
            get {
                return ResourceManager.GetString("DownloadError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Update files downloaded successfully.
        /// </summary>
        internal static string DownloadSuccessful {
            get {
                return ResourceManager.GetString("DownloadSuccessful", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Downloading files....
        /// </summary>
        internal static string FileDownload {
            get {
                return ResourceManager.GetString("FileDownload", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to new files found.
        /// </summary>
        internal static string FilesFoundCount {
            get {
                return ResourceManager.GetString("FilesFoundCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No files to replace : aborting update.
        /// </summary>
        internal static string NoNewFile {
            get {
                return ResourceManager.GetString("NoNewFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Opening KALUS.
        /// </summary>
        internal static string OpenKalus {
            get {
                return ResourceManager.GetString("OpenKalus", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Replacing files....
        /// </summary>
        internal static string ReplacingFiles {
            get {
                return ResourceManager.GetString("ReplacingFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to KALUS update has failed.
        /// </summary>
        internal static string UpdateFail {
            get {
                return ResourceManager.GetString("UpdateFail", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to KALUS has been updated.
        /// </summary>
        internal static string UpdateSuccess {
            get {
                return ResourceManager.GetString("UpdateSuccess", resourceCulture);
            }
        }
    }
}