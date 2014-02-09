﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using poshsecframework.Strings;

namespace poshsecframework.Interface
{
    public partial class frmSettings : Form
    {
        private FolderBrowserDialog dlgFolder = new FolderBrowserDialog();
        private OpenFileDialog dlgFile = new OpenFileDialog();
        private bool restart = false;

        private enum ModuleState
        { 
            Okay = 0,
            UpdatePending,
            Error
        }

        public frmSettings()
        {
            InitializeComponent();
            cmbScriptDefAction.SelectedIndex = 0;
            LoadSettings();
        }

        private void LoadSettings()
        {
            txtScriptDirectory.Text = Properties.Settings.Default.ScriptPath;
            txtGithubAPIKey.Text = Properties.Settings.Default.GithubAPIKey;
            txtModuleDirectory.Text = Properties.Settings.Default.ModulePath;
            txtPSExecPath.Text = Properties.Settings.Default.PSExecPath;
            txtSchFile.Text = Properties.Settings.Default.ScheduleFile;
            cmbScriptDefAction.SelectedIndex = Properties.Settings.Default.ScriptDefaultAction;
            bool firsttime = Properties.Settings.Default.FirstTime;
            if (firsttime)
            {
                cmbFirstTime.SelectedIndex = 0;
            }
            else
            {
                cmbFirstTime.SelectedIndex = 1;
            }
            ckNameCheck.Checked = Properties.Settings.Default.NameChecking;
            LoadModules();
        }

        private void SaveModules()
        {
            System.Collections.Specialized.StringCollection modsettings = new System.Collections.Specialized.StringCollection();
            foreach (ListViewItem lvw in lvwModules.Items)
            {
                String modstr = "";
                foreach (ListViewItem.ListViewSubItem subitm in lvw.SubItems)
                {
                    modstr += subitm.Text + "|";
                }
                if(modstr.Trim() != "")
                {
                    modstr = modstr.Substring(0, modstr.Length - 1);
                    modsettings.Add(modstr);
                }                
            }
            Properties.Settings.Default["Modules"] = modsettings;
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
        }

        private void LoadModules()
        {
            System.Collections.Specialized.StringCollection modsettings = Properties.Settings.Default.Modules;
            if (modsettings != null && modsettings.Count > 0)
            {
                foreach (String mod in modsettings)
                {
                    String[] modparts = mod.Split('|');
                    if (modparts != null && modparts.Length >= 3 && modparts.Length <= 4)
                    {
                        ListViewItem lvw = new ListViewItem();
                        lvw.Text = modparts[0];
                        for (int idx = 1; idx < modparts.Length; idx++)
                        {
                            lvw.SubItems.Add(modparts[idx]);
                        }
                        if (modparts.Length == 3)
                        {
                            lvw.SubItems.Add("");
                        }
                        ModuleState mstate = GetModuleState(modparts);
                        lvw.ImageIndex = (int)mstate;
                        lvwModules.Items.Add(lvw);
                        if (mstate != ModuleState.Okay)
                        {
                            tcSettings.SelectedTab = tbpModules;
                        }
                    }
                }
            }
        }

        private ModuleState GetModuleState(String[] modparts)
        {
            ModuleState rtn = ModuleState.Okay;
            String localpath = Path.Combine(Properties.Settings.Default.ModulePath, modparts[0]);
            if (Directory.Exists(localpath))
            {
                String[] files = Directory.GetFiles(localpath, "*.psd1", SearchOption.TopDirectoryOnly);
                if (files == null || files.Count() == 0)
                {
                    rtn = ModuleState.Error;
                }
                //Check Last Github Query  / Last Update Here
            }
            else
            {
                rtn = ModuleState.Error;
            }
            return rtn;
        }

        private void AddModule(String name, String location, String branch, DateTime lastcommit)
        {
            bool exists = false;
            int idx = 0;
            ListViewItem lvw = null;
            while (idx < lvwModules.Items.Count && !exists)
            {
                lvw = lvwModules.Items[idx];
                if (lvw.Text.ToLower() == name.ToLower() && lvw.SubItems[1].Text.ToLower() == location.ToLower())
                {
                    exists = true;
                }
                idx++;
            }
            if (exists)
            {
                lvw.SubItems[2].Text = branch;
                lvw.SubItems[3].Text = lastcommit.ToString(StringValue.TimeFormat);
            }
            else
            {
                lvw = new ListViewItem();
                lvw.Text = name;
                lvw.SubItems.Add(location);
                lvw.SubItems.Add(branch);
                lvw.SubItems.Add(lastcommit.ToString(StringValue.TimeFormat));
                lvw.ImageIndex = 0;
                lvwModules.Items.Add(lvw);
            }
            SaveModules();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (Save())
            {
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
        }

        private bool Save()
        {
            bool rtn = true;
            if (!Directory.Exists(txtScriptDirectory.Text))
            {
                if (MessageBox.Show(StringValue.ScriptPathError + StringValue.CreatePath, "Settings", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(txtScriptDirectory.Text);
                    }
                    catch (Exception e)
                    {
                        rtn = false;
                        MessageBox.Show(e.Message);
                    }
                }
                else
                {
                    rtn = false;
                }
            }
            if (!Directory.Exists(txtModuleDirectory.Text))
            {
                if (MessageBox.Show(StringValue.ModulePathError + StringValue.CreatePath, "Settings", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(txtModuleDirectory.Text);
                    }
                    catch (Exception e)
                    {
                        rtn = false;
                        MessageBox.Show(e.Message);
                    }
                }
                else
                {
                    rtn = false;
                }
            }
            if (Directory.Exists(txtScriptDirectory.Text) && Directory.Exists(txtModuleDirectory.Text))
            {
                Properties.Settings.Default["ScriptPath"] = txtScriptDirectory.Text;
                Properties.Settings.Default["ModulePath"] = txtModuleDirectory.Text;
                Properties.Settings.Default["ScriptDefaultAction"] = cmbScriptDefAction.SelectedIndex;
                Properties.Settings.Default["PSExecPath"] = txtPSExecPath.Text;
                Properties.Settings.Default["ScheduleFile"] = txtSchFile.Text;
                Properties.Settings.Default["GithubAPIKey"] = txtGithubAPIKey.Text;
                bool firsttime = false;
                if (cmbFirstTime.SelectedIndex == 0)
                {
                    firsttime = true;
                }
                Properties.Settings.Default["FirstTime"] = firsttime;
                Properties.Settings.Default["NameChecking"] = ckNameCheck.Checked;
                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();

            }
            return rtn;
        }

        private void btnBrowseScript_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(txtScriptDirectory.Text))
            {
                dlgFolder.SelectedPath = txtScriptDirectory.Text;
            }
            dlgFolder.Description = "Select the Script Directory.";
            if (dlgFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtScriptDirectory.Text = dlgFolder.SelectedPath;
            }
        }

        private void btnBrowseModule_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(txtModuleDirectory.Text))
            {
                dlgFolder.SelectedPath = txtModuleDirectory.Text;
            }
            dlgFolder.Description = "Select the Module Directory.";
            if (dlgFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtModuleDirectory.Text = dlgFolder.SelectedPath;
            }
        }

        private void btnBrowsePSExec_Click(object sender, EventArgs e)
        {
            dlgFile.Title = "Select the PSExec File.";
            dlgFile.FileName = "*psexec.exe";
            dlgFile.CheckFileExists = true;
            if (File.Exists(txtPSExecPath.Text))
            {
                dlgFile.InitialDirectory = new FileInfo(txtPSExecPath.Text).Directory.ToString();
            }                        
            if (dlgFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtPSExecPath.Text = dlgFile.FileName;
            }
        }

        private void btnBrowseSchFile_Click(object sender, EventArgs e)
        {
            dlgFile.Title = "Set Schedule File";
            dlgFile.FileName = "schedule.xml";
            dlgFile.Filter = "Extensible Markup Language (*.xml)|*.xml";
            dlgFile.CheckFileExists = false;
            if (File.Exists(txtSchFile.Text))
            {
                dlgFile.InitialDirectory = new FileInfo(txtSchFile.Text).Directory.ToString();
            }
            if (dlgFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtSchFile.Text = dlgFile.FileName;
            }
        }

        private void btnAddModule_Click(object sender, EventArgs e)
        {
            if (Save())
            {
                Interface.frmRepository frm = new frmRepository();
                if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    restart = frm.Restart;
                    AddModule(frm.RepositoryName, frm.LocationName, frm.Branch, frm.LastUpdate);
                }
                frm.Dispose();
                frm = null;
                if (restart)
                {
                    lblRestartRequired.Visible = true;
                }
            }            
        }

        public bool Restart
        {
            get { return restart; }
        }

        private void btnGithubHelp_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(StringValue.RateLimitURL);
                psi.UseShellExecute = true;
                psi.Verb = "open";
                System.Diagnostics.Process prc = new System.Diagnostics.Process();
                prc.StartInfo = psi;
                prc.Start();
                prc = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ckNameCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (ckNameCheck.Checked)
            {
                ckNameCheck.Text = "On";
            }
            else
            {
                ckNameCheck.Text = "Off";
            }
        }
    }
}
