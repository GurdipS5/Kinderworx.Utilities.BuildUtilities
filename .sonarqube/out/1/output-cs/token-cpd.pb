õ
oD:\Repositories\Utilities\Kinderworx.Utilities.BuildUtilities\Kinderworx.Utilities.BuildUtilities\BuildUtils.cs
	namespace 	

Kinderworx
 
. 
	Utilities 
. 
BuildUtilities -
{		 
public 

static 
class 

BuildUtils "
{ 
public 
static 
string 
Test !
(! "
)" #
{ 	
return 
$str 
; 
} 	
public 
static 
string 
GetProjectName +
(+ ,
), -
{ 	
Assembly 
assembly 
= 
Assembly  (
.( )
GetEntryAssembly) 9
(9 :
): ;
;; <
string   
assemblyPath   
=    !
assembly  " *
.  * +
Location  + 3
;  3 4
string## 
assemblyFileName## #
=##$ %
Path##& *
.##* +
GetFileName##+ 6
(##6 7
assemblyPath##7 C
)##C D
;##D E
return%% 
assemblyFileName%% #
;%%# $
}&& 	
public-- 
static-- 
string-- 
Helper-- #
(--# $
string--$ *
projectName--+ 6
)--6 7
=>--8 :
projectName--; F
.--F G
Replace--G N
(--N O
$str--O R
,--R S
$str--T W
)--W X
;--X Y
public33 
static33 
void33 
ZipDirectory33 '
(33' (
string33( .
	directory33/ 8
,338 9
string33: @
zipPath33A H
)33H I
{44 	
if55 
(55 
	Directory55 
.55 
Exists55  
(55  !
	directory55! *
)55* +
)55+ ,
{66 
ZipFile77 
.77 
CreateFromDirectory77 +
(77+ ,
	directory77, 5
,775 6
zipPath777 >
)77> ?
;77? @
}88 
else:: 
{;; 
	Directory<< 
.<< 
CreateDirectory<< )
(<<) *
	directory<<* 3
)<<3 4
;<<4 5
throw== 
new== &
DirectoryNotFoundException== 4
(==4 5
)==5 6
;==6 7
}>> 
}AA 	
publicII 
staticII 
stringII 
ExtractVersionII +
(II+ ,
stringII, 2
stdOutII3 9
)II9 :
{JJ 	
varLL 
withoutSpeechMarksLL "
=LL# $
stdOutLL% +
.LL+ ,
ReplaceLL, 3
(LL3 4
$strLL4 8
,LL8 9
$strLL: <
)LL< =
;LL= >
varOO !
withoutSquareBracketsOO %
=OO& '
withoutSpeechMarksOO( :
.OO: ;
ReplaceOO; B
(OOB C
$strOOC F
,OOF G
$strOOH J
)OOJ K
.PP 
ReplacePP 
(PP 
$strPP 
,PP 
$strPP  
)PP  !
;PP! "
varRR 
linesRR 
=RR !
withoutSquareBracketsRR -
.RR- .
SplitRR. 3
(RR3 4
$charRR4 7
)RR7 8
;RR8 9
stringTT 
sTT 
=TT 
linesTT 
[TT 
$numTT 
]TT 
;TT  
stringVV 
modifiedStringVV !
=VV" #
sVV$ %
.VV% &
ReplaceVV& -
(VV- .
$strVV. N
,VVN O
stringVVP V
.VVV W
EmptyVVW \
)VV\ ]
.VV] ^
TrimVV^ b
(VVb c
)VVc d
;VVd e
returnXX 
modifiedStringXX !
;XX! "
}ZZ 	
}[[ 
}\\ 