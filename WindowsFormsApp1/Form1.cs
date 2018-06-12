using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RPGClassLibrary;
namespace WindowsFormsApp1
{
	public partial class Form1 : Form
	{

		private volatile bool m_StopThread;
		
		public Player currentPlayer;
		public PlayerManager PM;
		//public JournalHandler JH;
		int playerId = 0;
		//RpgGameContext db;
		public Form1(int _playerId)
		{
			InitializeComponent();
			playerId = _playerId;
			JournalHandler.instance = new JournalHandler();

			using (var xdb = new RpgGameContext())
			{
				var c_player = xdb.Players.Where(p => p.Id == playerId).FirstOrDefault();
				
				currentPlayer = new Player()
				{
					Id = c_player.Id,
					PlayerName = c_player.PlayerName,
					SavesPath = c_player.SavesPath

				};
				PM = new PlayerManager(currentPlayer.PlayerName, new Stat(100), new Stat(100), 1, 0, 0);

				
			}

			Thread UIupdater = new Thread(new ThreadStart(UpdateUI))
			{
				IsBackground = true
			};
			UIupdater.Start();
		}

		

		private void Form1_Load(object sender, EventArgs e)
		{
			this.Owner.Enabled = false;
		}

		private void RecoverButton_Click(object sender, EventArgs e)
		{
			if (TavernTimer.Enabled == true)
			{
				TavernTimer.Stop();
			}
			else
			{
				TavernTimer.Start();
			}
		}

		private void GetQuestButon_Click(object sender, EventArgs e)
		{
			DisplayDialog();
		}

		private void LeaveTavernButton_Click(object sender, EventArgs e)
		{
			TavernTimer.Stop();
		}
		private void DisplayDialog()
		{

			if (PM.playerQuesting == QuestState.NotTaken)
			{
				QuestForm dlg = new QuestForm();

				// Show the dialog and determine the state of the 
				// DialogResult property for the form.
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					//you are leaving the tavern anyway
					//or not

					//TavernTimer.Stop();
					//you go to main page

					//you visit shop

					//OR start Quest

					MessageBox.Show("Accepted");
					PM.playerQuesting = QuestState.InProgress;
					using (var xdb = new RpgGameContext())
					{
						MessageBox.Show(dlg.QuestIDD.ToString());
						var currQuest = xdb.PlayerQuests.Where(p => p.QuestId == dlg.QuestIDD).FirstOrDefault();
						currQuest.QuestState = QuestState.InProgress;
						xdb.SaveChanges();
					}
					//if quest is finished - add in JR
					string qname = dlg.QuestNameProp;
					string qdesc = dlg.QuestDescriptionProp;
					MessageBox.Show(qname, qdesc);


					//Add in journalrecords
					using (var xdb = new RpgGameContext())
					{

						JournalRecord jr = new JournalRecord()
						{
							QuestDescription = qdesc,
							QuestName = qname
						};

						xdb.JournalRecords.Add(jr);
						xdb.SaveChanges();

						var i = xdb.PlayerRecords.Add(new PlayerRecord()
						{
							PlayerId = currentPlayer.Id,
							JournalRecordId = jr.Id
							//Player = new List<Player>(),
							//JournalRecord = new List<JournalRecord>()
						});
						
						xdb.SaveChanges();
						i.JournalRecord.Where(x => x.Id == jr.Id).FirstOrDefault().PlayerRecordId = i.Id;

						xdb.SaveChanges();



						var currntplayerjournal = xdb.PlayerRecords.Where(p => p.Id == playerId).ToList();

						foreach (var item in currntplayerjournal)
						{
							JournalHandler.instance.AddRecord(
								xdb.JournalRecords.Where(c => c.PlayerRecordId == item.Id).FirstOrDefault().QuestName,
								xdb.JournalRecords.Where(c => c.PlayerRecordId == item.Id).FirstOrDefault().QuestDescription
								);
						}
						//JournalHandler.instance.AddRecord(qname, qdesc);

					}

					//handle journal records

					QuestName.Text = qname;
					QuestDescrioptionlabel.Text = qdesc;

					foreach (var i in JournalHandler.instance.JournalRecords)
					{
						var si = listView1.Items.Add(i.Key);
						si.SubItems.Add(i.Value);
					}
					
				}
				else
				{
					//return to tavern continue drinking
					MessageBox.Show("Declined");

				}
			}
			else
			{
				MessageBox.Show("Already has a quest!");
			}

		}

		private void TavernTimer_Tick(object sender, EventArgs e)
		{
		
			if (PM.HP.CurrentValue >= PM.HP.BaseValue && PM.Stamina.CurrentValue < PM.Stamina.BaseValue)
			{
				PM.Stamina.CurrentValue = PM.Stamina.CurrentValue + 1.0;
			}
			if (PM.HP.CurrentValue < PM.HP.BaseValue && PM.Stamina.CurrentValue >= PM.Stamina.BaseValue)
			{
				PM.HP.CurrentValue = PM.HP.CurrentValue + 1.0;
			}
			if (PM.HP.CurrentValue < PM.HP.BaseValue && PM.Stamina.CurrentValue < PM.Stamina.BaseValue)
			{
				PM.HP.CurrentValue = PM.HP.CurrentValue + 1.0;
				PM.Stamina.CurrentValue = PM.Stamina.CurrentValue + 1.0;
			}
			if (PM.HP.CurrentValue >= PM.HP.BaseValue && PM.Stamina.CurrentValue >= PM.Stamina.BaseValue)
			{
				TavernTimer.Stop();
			}
		}

		private void UpdateUI()
		{
			while (!m_StopThread)
			{
				StaminatextBox.Text = PM.Stamina.CurrentValue.ToString();
				HPtextBox.Text = PM.HP.CurrentValue.ToString();
			}
			
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			//save data to saves

			//stopthread
		}

		private void Form1_FormClosed(object sender, FormClosedEventArgs e)
		{
			this.Owner.Enabled = true;
			m_StopThread = true;
		}

		public void SaveProgress()
		{
			//find path
			//openfile
			//rewrite it
			//savechanges
		}
	}
}
