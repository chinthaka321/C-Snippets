        private static void UpdateComplianceListValuesSSOM(string siteUrl,string listTitle,uint noOfItemsPerBatch)
        {
            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate ()
                {
                    using (SPSite elevatedSite = new SPSite(siteUrl))
                    {
                        using (SPWeb web = elevatedSite.OpenWeb())
                        {
                            SPList complianceList = web.Lists.TryGetList(listTitle);

                            ContentIterator ci = new ContentIterator("Reading all items.");

                            SPQuery query = new SPQuery();
                            // Ensuring that all users come under List view Threshold. (Inculuding farm admins / box administrators)
                            query.QueryThrottleMode = SPQueryThrottleOption.Override;
                            // No of Items read in a batch. But it should be less than List View threshold
                            query.RowLimit = noOfItemsPerBatch;

                            //** Note: When apply this where condition it throws an error when executing
                            //**      So program doesn't work.
                            //query.Query = string.Concat(
                            //  "<Where>",
                            //    "<IsNull><FieldRef Name='Title' /></IsNull>",
                            //    //"<Or><IsNull><FieldRef Name='IncidentBusinessUnit' /></IsNull>",
                            //    //"<Or><IsNull><FieldRef Name='IncidentLocation1' /></IsNull>",
                            //    //"<Or><IsNull><FieldRef Name='IncidentLocation2' /></IsNull>", 
                            //    //"</Or>", 
                            //    //"</Or>", 
                            //    //"</Or>", 
                            //  "</Where>");                            

                            //** Note: When apply filter fields item.SystemUpdate doesn't work
                            //**        So had to remove view only fields query
                            //query.ViewFields = string.Concat(
                            //    "<FieldRef Name='ID' />",
                            //    "<FieldRef Name='Title' />",
                            //    "<FieldRef Name='IncidentBusinessUnit' />",
                            //    "<FieldRef Name='IncidentLocation1' />",
                            //    "<FieldRef Name='IncidentLocation2' />");
                            //query.ViewFieldsOnly = true;

                            // Not required, Include for faster output
                            query.Query = query.Query + ContentIterator.ItemEnumerationOrderByID;
                            // Start processing list items
                            ci.ProcessListItems(complianceList, query, ProcessItemsCollection, ProcessErrorItemsCollection);

                            Console.WriteLine("\nBatch count:" + NoOfBatch + "\n\nTotal number of items read: " + NoOfItemsRead);
                            SimpleLog.Info("\nBatch count:" + NoOfBatch + "\n\nTotal number of items read: " + NoOfItemsRead);
                        }
                    }
                });
            }
            catch(FileNotFoundException ex)
            {
                SimpleLog.Log(ex);
                throw new Exception(ex.Message);
            }
        }
       
       
              /// <summary>
        /// Process error list items
        /// </summary>
        /// <param name="listItems"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        private static bool ProcessErrorItemsCollection(SPListItemCollection listItems, Exception ex)
        {
            SimpleLog.Error("Error item collection generated.");
            SimpleLog.Error(ex.ToString());

            foreach (SPListItem item in listItems)
            {
                SimpleLog.Info("Error updating Item :" + item["Title"]);
            }

            NoOfException++;
            return true;
        }

        /// <summary>
        /// Process retrieved list items
        /// </summary>
        /// <param name="listItems"></param>
        private static void ProcessItemsCollection(SPListItemCollection listItems)
        {
            Console.WriteLine("Processing batch...");

            string fieldNameTitle = "Title";
            string fieldNameReviewerClosed = "IncidentReviewerClosed";
            string fieldNameActionCompleted = "IncidentActionCompleted";
            string fieldNameCreated = "Created";

            int itemCount = 0;

            // Go through items and update the item
            foreach (SPListItem item in listItems)
            {
                try
                {
                    Console.WriteLine("Processing Item " + item[fieldNameTitle]);
                    bool flagUpdateItem = false;

                    // Get the items before the filter date
                    if (Convert.ToDateTime(item[fieldNameCreated]) < filterDate)
                    {
                        // Update the item if it's not closed or action not completed
                        if (item[fieldNameActionCompleted] == null || item[fieldNameActionCompleted].ToString().ToUpper().Contains("No".ToUpper()))
                        {
                            item[fieldNameActionCompleted] = "Yes";
                            flagUpdateItem = true;
                        }
                        if (item[fieldNameReviewerClosed] == null || !(item[fieldNameReviewerClosed].ToString().ToUpper().Contains("Yes".ToUpper())))
                        {
                            item[fieldNameReviewerClosed] = "Yes";
                            flagUpdateItem = true;
                        }
                        if (flagUpdateItem)
                        {
                            //** TODO UNCOMENT THESE TO DISABLE EVENT FIRING
                            using (DisabledItemEventsScope scope = new DisabledItemEventsScope())
                            {
                                item.SystemUpdate(false);
                                updatedItemCount++;
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    SimpleLog.Error(ex.ToString());
                    SimpleLog.Error("Error updating item Title:" + item[fieldNameTitle] + " ID:" + item["ID"]);
                }

                itemCount++;
            }

            NoOfBatch++;
            NoOfItemsRead += listItems.Count;

            Console.WriteLine("Number of Items Read: " + NoOfItemsRead);
            SimpleLog.Info("\nBatch count: " + NoOfBatch + "\n\nTotal number of items read: " + NoOfItemsRead);
        }
