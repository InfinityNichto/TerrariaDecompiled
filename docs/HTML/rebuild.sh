#!/bin/sh
#
# rebuild.sh: rebuild hypertext with the previous context.
#
# Usage:
#	% sh rebuild.sh
#
cd /home/runner/work/TerrariaDecompiled/TerrariaDecompiled/source && GTAGSCONF=':skip=HTML/,HTML.pub/,tags,TAGS,ID,y.tab.c,y.tab.h,gtags.files,cscope.files,cscope.out,cscope.po.out,cscope.in.out,SCCS/,RCS/,CVS/,CVSROOT/,{arch}/,autom4te.cache/,*.orig,*.rej,*.bak,*~,#*#,*.swp,*.tmp,*_flymake.*,*_flymake,*.o,*.a,*.so,*.lo,*.zip,*.gz,*.bz2,*.xz,*.lzh,*.Z,*.tgz,*.min.js,*min.css:langmap=c\:.c.h,yacc\:.y,asm\:.s.S,java\:.java,cpp\:.c++.cc.hh.cpp.cxx.hxx.hpp.C.H,php\:.php.php3.phtml:' htags -g -s -a -n -v -w -t Terraria-v1.4.4.9 /home/runner/work/TerrariaDecompiled/TerrariaDecompiled/doxygen/html
