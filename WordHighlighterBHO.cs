﻿using Microsoft.Win32;
using mshtml;
using SHDocVw;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;

namespace InternetExplorerExtension
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("D40C654D-7C51-4EB3-95B2-1E23905C2A2D")]
    [ProgId("MyBHO.WordHighlighter")]
    public class WordHighlighterBHO : IObjectWithSite, IOleCommandTarget
    {
        const string DefaultTextToHighlight = "browser";

        IWebBrowser2 browser;
        private object site;



        #region Highlight Text
        void OnDocumentComplete(object pDisp, ref object URL)
        {
            try
            {
                // @Eric Stob: Thanks for this hint!
                // This will prevent this method being executed more than once.
                if (pDisp != this.site)
                    return;

                var document2 = browser.Document as IHTMLDocument2;
                var document3 = browser.Document as IHTMLDocument3;

                var window = document2.parentWindow;
                window.execScript(@"function FncAddedByAddon() { alert('Message added by addon 5 " + "" + @"'); }");

                Queue<IHTMLDOMNode> queue = new Queue<IHTMLDOMNode>();
                foreach (IHTMLDOMNode eachChild in document3.childNodes)
                    queue.Enqueue(eachChild);

                while (queue.Count > 0)
                {
                    // replacing desired text with a highlighted version of it
                    var domNode = queue.Dequeue();

                    var textNode = domNode as IHTMLDOMTextNode;
                    if (textNode != null)
                    {
                        if (textNode.data.Contains(TextToHighlight))
                        {
                            var newText = textNode.data.Replace(TextToHighlight, "<span style='background-color: yellow; cursor: hand;' onclick='javascript:FncAddedByAddon()' title='Click to open script based alert window.'>" + TextToHighlight + "</span>");
                            var newNode = document2.createElement("span");
                            newNode.innerHTML = newText;
                            domNode.replaceNode((IHTMLDOMNode)newNode);
                        }
                    }
                    else
                    {
                        // adding children to collection
                        var x = (IHTMLDOMChildrenCollection)(domNode.childNodes);
                        foreach (IHTMLDOMNode eachChild in x)
                        {
                            if (eachChild is mshtml.IHTMLScriptElement)
                                continue;
                            if (eachChild is mshtml.IHTMLStyleElement)
                                continue;

                            queue.Enqueue(eachChild);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }
        #endregion

        private void log(string text)
        {
            File.AppendAllText(@"C:\kopylov\inbox\log.txt", text + Environment.NewLine);
        }

        #region Load and Save Data
        static string TextToHighlight = DefaultTextToHighlight;
        public static string RegData = "Software\\MyIEExtension";

        [DllImport("ieframe.dll")]
        public static extern int IEGetWriteableHKCU(ref IntPtr phKey);

        private static void SaveOptions()
        {
            // In IE 7,8,9,(desktop)10 tabs run in Protected Mode
            // which prohibits writes to HKLM, HKCU.
            // Must ask IE for "Writable" registry section pointer
            // which will be something like HKU/S-1-7***/Software/AppDataLow/
            // In "metro" IE 10 mode, tabs run in "Enhanced Protected Mode"
            // where BHOs are not allowed to run, except in edge cases.
            // see http://blogs.msdn.com/b/ieinternals/archive/2012/03/23/understanding-ie10-enhanced-protected-mode-network-security-addons-cookies-metro-desktop.aspx
            IntPtr phKey = new IntPtr();
            var answer = IEGetWriteableHKCU(ref phKey);
            RegistryKey writeable_registry = RegistryKey.FromHandle(
                new Microsoft.Win32.SafeHandles.SafeRegistryHandle(phKey, true)
            );
            RegistryKey registryKey = writeable_registry.OpenSubKey(RegData, true);

            if (registryKey == null)
                registryKey = writeable_registry.CreateSubKey(RegData);
            registryKey.SetValue("Data", TextToHighlight);

            writeable_registry.Close();
        }
        private static void LoadOptions()
        {
            // In IE 7,8,9,(desktop)10 tabs run in Protected Mode
            // which prohibits writes to HKLM, HKCU.
            // Must ask IE for "Writable" registry section pointer
            // which will be something like HKU/S-1-7***/Software/AppDataLow/
            // In "metro" IE 10 mode, tabs run in "Enhanced Protected Mode"
            // where BHOs are not allowed to run, except in edge cases.
            // see http://blogs.msdn.com/b/ieinternals/archive/2012/03/23/understanding-ie10-enhanced-protected-mode-network-security-addons-cookies-metro-desktop.aspx
            IntPtr phKey = new IntPtr();
            var answer = IEGetWriteableHKCU(ref phKey);
            RegistryKey writeable_registry = RegistryKey.FromHandle(
                new Microsoft.Win32.SafeHandles.SafeRegistryHandle(phKey, true)
            );
            RegistryKey registryKey = writeable_registry.OpenSubKey(RegData, true);

            if (registryKey == null)
                registryKey = writeable_registry.CreateSubKey(RegData);
            registryKey.SetValue("Data", TextToHighlight);

            if (registryKey == null)
            {
                TextToHighlight = DefaultTextToHighlight;
            }
            else
            {
                TextToHighlight = (string)registryKey.GetValue("Data");
            }
            writeable_registry.Close();
        }
        #endregion

        [Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
        [InterfaceType(1)]
        public interface IServiceProvider
        {
            int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject);
        }



        #region Implementation of IObjectWithSite
        int IObjectWithSite.SetSite(object site)
        {
            this.site = site;

            if (site != null)
            {
                LoadOptions();

                var serviceProv = (IServiceProvider)this.site;
                var guidIWebBrowserApp = Marshal.GenerateGuidForType(typeof(IWebBrowserApp)); // new Guid("0002DF05-0000-0000-C000-000000000046");
                var guidIWebBrowser2 = Marshal.GenerateGuidForType(typeof(IWebBrowser2)); // new Guid("D30C1661-CDAF-11D0-8A3E-00C04FC9E26E");
                IntPtr intPtr;
                serviceProv.QueryService(ref guidIWebBrowserApp, ref guidIWebBrowser2, out intPtr);

                browser = (IWebBrowser2)Marshal.GetObjectForIUnknown(intPtr);

                ((DWebBrowserEvents2_Event)browser).DocumentComplete +=
                    new DWebBrowserEvents2_DocumentCompleteEventHandler(this.OnDocumentComplete);
            }
            else
            {
                ((DWebBrowserEvents2_Event)browser).DocumentComplete -=
                    new DWebBrowserEvents2_DocumentCompleteEventHandler(this.OnDocumentComplete);
                browser = null;
            }
            return 0;
        }
        int IObjectWithSite.GetSite(ref Guid guid, out IntPtr ppvSite)
        {
            IntPtr punk = Marshal.GetIUnknownForObject(browser);
            int hr = Marshal.QueryInterface(punk, ref guid, out ppvSite);
            Marshal.Release(punk);
            return hr;
        }
        #endregion
        #region Implementation of IOleCommandTarget
        int IOleCommandTarget.QueryStatus(IntPtr pguidCmdGroup, uint cCmds, ref OLECMD prgCmds, IntPtr pCmdText)
        {
            return 0;
        }
        int IOleCommandTarget.Exec(IntPtr pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            try
            {
                // Accessing the document from the command-bar.
                var document = browser.Document as IHTMLDocument2;
                var window = document.parentWindow;
                var result = window.execScript(@"alert('You will now be allowed to configure the text to highlight...');");

                var form = new HighlighterOptionsForm();
                form.InputText = TextToHighlight;
                if (form.ShowDialog() != DialogResult.Cancel)
                {
                    TextToHighlight = form.InputText;
                    SaveOptions();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return 0;
        }
        #endregion

        #region Registering with regasm
        public static string RegBHO = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Browser Helper Objects";
        public static string RegCmd = "Software\\Microsoft\\Internet Explorer\\Extensions";

        [ComRegisterFunction]
        public static void RegisterBHO(Type type)
        {
            string guid = type.GUID.ToString("B");

            // BHO
            {
                RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(RegBHO, true);
                if (registryKey == null)
                    registryKey = Registry.LocalMachine.CreateSubKey(RegBHO);
                RegistryKey key = registryKey.OpenSubKey(guid);
                if (key == null)
                    key = registryKey.CreateSubKey(guid);
                key.SetValue("Alright", 1);
                registryKey.Close();
                key.Close();
            }

            // Command
            {
                RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(RegCmd, true);
                if (registryKey == null)
                    registryKey = Registry.LocalMachine.CreateSubKey(RegCmd);
                RegistryKey key = registryKey.OpenSubKey(guid);
                if (key == null)
                    key = registryKey.CreateSubKey(guid);
                key.SetValue("ButtonText", "Highlighter options");
                key.SetValue("CLSID", "{1FBA04EE-3024-11d2-8F1F-0000F87ABD16}");
                key.SetValue("ClsidExtension", guid);
                key.SetValue("Icon", "");
                key.SetValue("HotIcon", "");
                key.SetValue("Default Visible", "Yes");
                key.SetValue("MenuText", "&Highlighter options");
                key.SetValue("ToolTip", "Highlighter options");
                //key.SetValue("KeyPath", "no");
                registryKey.Close();
                key.Close();
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterBHO(Type type)
        {
            string guid = type.GUID.ToString("B");
            // BHO
            {
                RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(RegBHO, true);
                if (registryKey != null)
                    registryKey.DeleteSubKey(guid, false);
            }
            // Command
            {
                RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(RegCmd, true);
                if (registryKey != null)
                    registryKey.DeleteSubKey(guid, false);
            }
        }
        #endregion
    }
}