//current TODO List : 
//make a worker that do clear fraud detection every 24 hours 
//make a repository interface for rules as IRulesRepository every rule need to follow that interface 
//add business logic with rules 
//with mock data flag if fraud detects 
//improvements note other than every 24 hour clean cheks every payment goes trough detection with async 
- fraud function like we could hava a redis cache in server life-time could be 1-2 minutes and if same -amount of payment goes trough twice we flagged as fraud

//I can use both EF and DAPPER ef could make easy cruds like flagging etc. but while we checking the transactions DAPPER could make faster optimized queries with indexing cardNo's etc.
as in : 
select location , amount 
from transaction as t 
left join user 
where user.cardNo == t.cardNo
""
this is sample not a funtional code with something like this we can check every persons transactions and
with the infos about locations , amounts , and probably avarage amounts i can detect wheter it is a fraud or not to flag it to the sql via ef for fast Updates .  

--Note to myself update the Readme before publishing anything 

-Ali Eren Özer