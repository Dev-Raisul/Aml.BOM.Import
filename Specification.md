OBJECTIVE: 
Provide functionality to integrate new bills of materials into Sage and, as necessary, create new inventory  items in Sage. 
APPLICATION OVERVIEW: 
The application will create a workspace where the user(s) can review BOM data requiring additional  The user will then be able to create new items and integrate new bills of materials. 
information required to integrate. The user will be able to filter the list of working items and use features  such as ‘copy from another item’ or ‘copy value to all items’ to efficiently fill in the required information.  


DETAILED FUNCTIONALITY: 
• Settings – DB connections and Sage credentials, report files 
• User Interface: 
o The user opens the utility and is presented with all pending BOM uploads and has the  ability to select and add a new BOM Import file.  
▪ Upon Upload – If a new BOM Import file is uploaded to SQL the records (parents  and components) are validated against the Sage item database 
▪ All pending BOMs that were previously uploaded are also run through the validation  logic 
▪ The user will also have a [Revalidate] button to have the pending BOMs re 
validated. 
▪ The import file will be rejected if the filename is already in the imported BOMs  table. 
▪ Any BOM that already exists (parent item is in the bill of materials header or in the  imported BOM table) it and all component rows will be marked as ‘duplicated’ 
signifying that they are duplicated BOMs and will be ignored by the system. 
o Tabs or Views – upon completion of this step 1 upload data will be presented in multiple views or tabs as follows: 
▪ New Buy Items – a reporting view that presents any buy items that are new, not in  Sage. 
▪ New Make Items – a working view that presents any new make items allowing the  user to efficiently fill in the required data to create the new make items 
▪ New BOMs – a reporting view that shows the total number of BOMs pending along  with the number of BOMs that can currently be integrated, the number of BOMs  requiring the creation of new make items and the number of BOMs requiring new  buy items 
▪ Integrated BOMs – presenting BOMs previously integrated via the utility 
▪ Duplicate BOMs – items identified as duplcates (see above) 
o Buy Item View – 
▪ General - If any buy items are identified as missing (new buy items) the BOM will be  rejected and made available for reporting so that the procurement team can create the items in Sage. 
▪ The new buy item data is displayed in a table 
▪ A print button allows the list to be printed 
o Make Item View - For new ‘Make’ items the application will present a table listing the new  make items 
▪ Columns (the list can be sorted by any column):
Column 
Default 
Editable
Import File Name 
System 
No
Import File Date Imported 
System 
No
Item Code 
From Import File 
No
Item Description 
From Import File 
Yes
Product Line 
Blank (lookup to sage) 
Yes
Product Type 
F (finished goods) 
Yes
Procurement 
M (make) 
Yes
Standard Unit of Measure 
EACH 
Yes
Sub Product Family 
Blank (lookup to sage) 
Yes
Staged Item 
Unchecked 
Yes
Coated 
Unchecked 
Yes
Golden Standard 
Unchecked 
Yes



▪ Make Item Interaction 
• Filters – 
o Import File Name – optional 
o Import File Date  
o The user can filter the list using the item code field or ask to see all  items that have been edited or all items that have not been edited  (are missing data) 
o Item filter will use special characters: 
▪ % - representing zero, one or multiple characters and used at  
the beginning or end of a search string 
▪ ? – representing a single character 
o Example: ACL5??LS40% would bring up all new make items which  start with ACL5 in the first 4 characters, any value in the 5th and 6th characters and LS40 in the 7th to 10th characters and then any set of  characters, or no characters, from the 11th position on 
o Integrated – the resulting list presented with this option toggled on is  ‘VIEW ONLY’ with no edits or other actions affecting the information.  This view will include the date integrated and the integrated by  
windows user fields. 
• Once filtered the user could then edit the values for any of the fields other  than the item code such that these would be the values to then integrate into Sage 
• Upon editing a value for one item the system will ask whether that value  should be copied to all of the currently filtered items.  
o Yes – if the user responds ‘Yes’ the value will be populated in the  selected column for all records currently appearing in the filter
o No – if the user responds ‘No’ then the value will only be populated  
for the current row. At this point any edits to any row for the current  
column will no longer prompt asking to copy the value to the other  
records 
o Right click on an edited cell and the user will be presented a menu  
with the following options: 
▪ Copy to all filtered items currently blank 
▪ Copy to all filtered items (regardless of whether other data  
already exists for the column) 
▪ Clear for all filtered items 
• Other actions 
o Copy From Item – search the sage item master table to select an item  
from which to copy all (except description) filtered items. The search  
window will show the same fields displayed in the columns 
o Clear - A button will exist to clear all user edited data for all items in  
the current filter or all items if not filtered.  
o Integrated - A button will exist to ‘Integrate’ all New Make Items  
which have had the product line field updated 
▪ New BOM View – the statistics are presented and the user can then select to  ‘INTEGRATE’ all eligible BOMs. Upon integrating a dialog window will appear stating  the number of BOMs integrated and the number of components and comment lines  (notes) integrated. 
o Business Rules 
▪ BOMs will not integrate if: 
• Contains a new buy item 
• Contains an unintegrated new make item 
• Already exists in Sage 
▪ New Make Items will not integrate if: 
• Product Line is not populated 
▪ New Buy Items – will be created outside of this utility and are presented for  reporting purposes 
• Development Notes 
o isBOMImport_Bills - The Imported BOMs will be loaded to a new table and contain the  data from the BOM Import file plus 
▪ Import File Name 
▪ Import Date 
▪ Import Windows User 
▪ Status
• Validated 
• Integrated 
• New Buy Item 
• New Make Item 
▪ Date Validated 
▪ Date Integrated 
o isBOMImport_NewMakeItems - New Make Items are loaded to this table. Any new make  item will only exist in this table once though it may exist on multiple other parent items.  This table will hold the relevant information from the BOM Import File (the first  occurrence) plus 
▪ Date created 
▪ Created Windows User 
▪ Date modified 
▪ Modified Windows User 
▪ Item Description - from first occurrence of the BOM Interface File or as edited if the  user edits the description 
▪ Product Line – blank or as edited 
▪ Product Type – as edited 
▪ Procurement – as edited 
▪ Standard Unit of Measure – as edited 
▪ Sub Product Family – as edited 
▪ Staged Item – as edited 
▪ Coated – as edited 
▪ Golden Standard – as edited 
▪ Date Integrated – the date that the item was integrated into Sage creating a new  make item 
▪ Integrated by Windows User ID 
LIMITATIONS AND EXCLUSIONS: 
• No edit logs 
• No roll-back functionality 
• Windows 10 or 11 workstation required 
• Single User Application
