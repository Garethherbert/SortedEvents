export const brands={
 celebrations:{key:'celebrations',name:'BigDaySorted',tagline:'Big day? Sorted.',types:['Wedding','Anniversary','Birthday','Charity fundraiser','Awards night','Corporate event','Graduation','Community celebration'],newLabel:'New celebration',defaultType:'Wedding'},
 live:{key:'live',name:'LiveDaysSorted',tagline:'Every stage. Every day. Sorted.',types:['Concert','Festival'],newLabel:'New live event',defaultType:'Concert'}
};

// Proposed commercial allowances. Billing and capacity enforcement are not enabled.
export const livePlans=[
 {id:'live-trial',name:'Trial',audience:'local',price:'Free',events:1,seats:1,contributors:0,storage:0,detail:'One local event to explore; no hosted collaboration'},
 {id:'live-pro',name:'Local Pro',audience:'local',price:'£79.99 once',events:null,seats:1,contributors:0,storage:0,detail:'Unlimited local plans; purchased version retained; 12 months of updates'},
 {id:'festival-pass',name:'Festival Pass',audience:'festival',eventTypes:['Festival'],price:'£199.99 / festival',events:1,seats:3,contributors:100,viewers:1000,storage:30,termMonths:12,maxDays:14,maxStages:5,detail:'One festival edition; 100 free contributing users; 3 organisers; 30 GB event storage'},
 {id:'festival-plus',name:'Festival Plus Pass',audience:'festival',eventTypes:['Festival'],price:'£349.99 / festival',events:1,seats:5,contributors:300,viewers:3000,storage:75,termMonths:12,maxDays:14,maxStages:5,detail:'One festival edition; 300 free contributing users; 5 organisers; 75 GB event storage'},
 {id:'concert-premium',name:'Concert Premium',audience:'concert',eventTypes:['Concert'],price:'£24.99 / month',annual:'£249.99 / year',events:5,seats:2,contributors:10,viewers:250,storage:20,detail:'5 active concerts; 10 free contributors per concert; 2 organisers; 20 GB account storage'},
 {id:'concert-plus',name:'Concert Premium Plus',audience:'concert',eventTypes:['Concert'],price:'£49.99 / month',annual:'£499.99 / year',events:15,seats:3,contributors:15,viewers:500,storage:50,detail:'15 active concerts; 15 free contributors per concert; 3 organisers; 50 GB account storage'},
 {id:'concert-business',name:'Concert Business',audience:'concert',eventTypes:['Concert'],price:'£79.99 / month',annual:'£799.99 / year',events:30,seats:5,contributors:25,viewers:1000,storage:100,detail:'30 active concerts; 25 free contributors per concert; 5 organisers; 100 GB account storage'}
];

export function renderLivePricing(value){for(const group of ['festival','concert','local'])value=value.replace('<!--'+group.toUpperCase()+'_PLANS-->',livePlans.filter(p=>p.audience===group).map(p=>`<article class="card"><h3>${p.name}</h3><div class="price">${p.price}</div>${p.annual?'<p>Or '+p.annual+'.</p>':''}<p>${p.detail}.</p><ul>${p.audience==='festival'?'<li>'+p.maxDays+' days · '+p.maxStages+' stages</li><li>'+p.termMonths+' months of hosted access · no auto-renewal</li>':''}${p.viewers?'<li>'+p.viewers.toLocaleString('en-GB')+' read-only viewers '+(p.audience==='concert'?'per concert':'per festival')+'</li>':''}${p.audience==='local'?'<li>No hosted collaboration or cloud storage</li>':'<li>Contributors can update assigned work</li><li>Native participant access planned</li>'}</ul></article>`).join(''));return value;}
