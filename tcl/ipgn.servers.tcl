#Script to interact with servers at a shell-level via IRC using an eggdrop bot
#Includes updating, restarting, etc
#Demo syncing
#Easy publication of files to multiple servers

lappend auto_path {/usr/lib}
package require Thread

set adminchan "#ipgn-tf2"
set regexRB "\[{}\]"

bind pub - @demosync serverbot:demosync
bind pub - @update serverbot:update
bind pub - @restart serverbot:restart
#bind pub n @pushcfg serverbot:pushconfig

bind pub n @addserver serverbot:newserver
bind pub n @delserver serverbot:delserver
bind pub - @listservers serverbot:listservers

bind pub - @help serverbot:help

bind pub n @release serverbot:releasethread
bind pub n @threads serverbot:showthreads

proc serverbot:demosync {nick uhost hand chan args} {
	global regexRB servers serverarray
	regsub -all $regexRB $args "" args
	serverbot:loadservers
	putlog "$nick ($uhost) issued command @demosync with args $args"
	if {(($args != "") && ([lsearch $servers $args] >= 0)) || ($args == "all")} {
		switch $args {
			all {
				set numServers [llength $servers]
				set x 0
				msgChan $chan Grabbing all demos from all servers
				while {$x < $numServers} {
					set server [lindex $servers $x]
					set serverip $serverarray($server)
					
					catch { exec /usr/bin/rsync --remove-sent-files -v steam@$server:/games/tf2/orangebox/tf/*.zip /data/gamefiles/tf2/demos/autorecord/$serverip/ } result
					
					if {([lsearch $result failed:] >= 0) || ($servers == "")} {
						msgChan $chan No demos to sync from $server
					} else {
						set rsynced [split $result "\n"]
						set democount 0
						set rsyncItems [llength $rsynced]
						
						while {$democount <= $rsyncItems} {
							if {[lindex $rsynced $democount] == ""} {
								break
							}
							putlog "Demo $democount synced: [lindex $rsynced $democount]"
							msgChan $chan Demo synced ($server): [lindex $rsynced $democount]
							incr democount
						}
						#msgChan $chan Demos from $server: $demos
					}
					incr x
				}
			}
			default {
				msgChan $chan Grabbing demos from $args
				set serverip $serverarray($args)
				
				catch { exec /usr/bin/rsync --remove-sent-files -v steam@$args:/games/tf2/orangebox/tf/*.zip /data/gamefiles/tf2/demos/autorecord/$serverip/ } result
				
				if {[lsearch $result failed:] >= 0} {
					msgChan $chan No demos to sync from $args
				} else {
					set rsynced [split $result "\n"]
					set democount 0
					set rsyncItems [llength $rsynced]
					
					while {$democount <= $rsyncItems} {
						if {[lindex $rsynced $democount] == ""} {
							break
						}
						putlog "Demo $democount synced: [lindex $rsynced $democount]"
						msgChan $chan Demo synced ($args): [lindex $rsynced $democount]
						#lappend demos { $democount: [lindex $rsynced $democount] }
						incr democount
					}
				}
			}
		}
	} else {
		msgChan $chan Invalid parameters specified. Use @demosync all, @demosync <server>. Servers found using @listservers
	}
}

proc serverbot:update {nick uhost hand chan args} {
	global regexRB servers
	regsub -all $regexRB $args "" args
	putlog "$nick ($uhost) issued command @update with args $args"
	if {(($args != "") && ([lsearch $servers $args] >= 0)) || ($args == "all")} {
		serverbot:loadservers
		switch $args {
			all {
				msgChan $chan Updating all servers
				foreach servername $servers {
					#ssh_cmd steam $servername "/games/ipgn/bin/update_tf2"
					set updateThreadId [thread::create {
						proc serverbot:threadupdate {host} {
							set mainThread [lindex [thread::names] [expr [llength [thread::names]] - 1]]
							thread::send $mainThread [list msgChan \#ipgn\-tf2 Updating servers on $host]
							
							catch { exec ssh steam@$host ---c "/games/ipgn/bin/update_tf2" } result
							set resultList [split $result "\n"]
							
							if {[lsearch -regexp $resultList .*up.*to.*date.*] >= 0} {
								thread::send $mainThread [list msgChan \#ipgn\-tf2 $host SRCDS Installation is up to date]
							} elseif {[lsearch -regexp $resultList .*Getting.*version.*(\d+).*] >= 0} {
								thread::send $mainThread [list msgChan \#ipgn\-tf2 $host Had to update HLDSUpdateTool. Running update again]
								
								catch { exec ssh steam@$host ---c "/games/ipgn/bin/update_tf2" } result2
								set resultList [split $result2 "\n"]
								
								if {[lsearch -regexp $resultList .*up.*to.*date.*] >= 0} {
									thread::send $mainThread [list msgChan \#ipgn\-tf2 $host SRCDS Installation is up to date]
								}
							} else {
								thread::send $mainThread [list msgChan \#ipgn\-tf2 Something weird just happened. Update may or may not have worked]
							}
							thread::send $mainThread [list msgChan \#ipgn\-tf2 All servers on $host have been updated]
							thread::release
						}
						thread::wait
					}]; #create thread for updating a server and wait for execution
					thread::configure $updateThreadId -unwindonerror 1
					thread::send -async $updateThreadId [list serverbot:threadupdate $servername]
					lappend updateIds $updateThreadId
					putlog "Thread id $updateThreadId has been created"
				}
				putlog "All thread IDs: $updateIds"
				#msgChan $chan debug: threads started for update: $updateIds
			}
			default {
				if {([lsearch $servers $args] >= 0)} {
					set updateThreadId [thread::create {
						proc serverbot:threadupdate {host} {
							set mainThread [lindex [thread::names] [expr [llength [thread::names]] - 1]]
							thread::send $mainThread [list msgChan \#ipgn\-tf2 Updating servers on $host]
							
							catch { exec ssh steam@$host ---c "/games/ipgn/bin/update_tf2" } result
							set resultList [split $result "\n"]
							
							if {[lsearch -regexp $resultList .*up.*to.*date.*] >= 0} {
								thread::send $mainThread [list msgChan \#ipgn\-tf2 $host SRCDS Installation is up to date]
							} elseif {[lsearch -regexp $resultList .*Getting.*version.*] >= 0} {
								thread::send $mainThread [list msgChan \#ipgn\-tf2 $host Had to update HLDSUpdateTool. Running update again]
								
								catch { exec ssh steam@$host ---c "/games/ipgn/bin/update_tf2" } result2
								set resultList [split $result2 "\n"]
								
								if {[lsearch -regexp $resultList .*up.*to.*date.*] >= 0} {
									thread::send $mainThread [list msgChan \#ipgn\-tf2 $host SRCDS Installation is up to date]
								}
							} else {
								thread::send $mainThread [list msgChan \#ipgn\-tf2 Something weird just happened. Update may or may not have worked]
							}
							thread::send $mainThread [list msgChan \#ipgn\-tf2 All servers on $host have been updated]
							thread::release
						}
						thread::wait
					}]; #create thread for updating a server and wait for execution
					thread::configure $updateThreadId -unwindonerror 1
					thread::send -async $updateThreadId [list serverbot:threadupdate $args]
					putlog "Thread $updateThreadId created for updating"
				} else {
					msg $chan Invalid server specified. Servers: $servers
				}
			}
		}
	} else {
		msgChan $chan No parameters specified. Use @update all, @update <server>. Servers found using @listservers
	}
}

proc serverbot:restart {nick uhost hand chan args} {
	global regexRB servers
	regsub -all $regexRB $args "" args
	putlog "$nick ($uhost) issued command @restart with args $args"
	if {(($args != "") && ([lsearch $servers $args] >= 0)) || ($args == "all")} {
		serverbot:loadservers
		switch $args {
			all {
				msgChan $chan Restarting all servers
				foreach servername $servers {
					catch { exec ssh steam@$servername ---c "/games/ipgn/bin/restart_tf2" } result
				}
				msgChan $chan All servers have been restarted
			}
			default {
				if {([lsearch $servers $args] >= 0)} {
					msgChan $chan Restarting servers on $args
					catch { exec ssh steam@$args ---c "/games/ipgn/bin/restart_tf2" } result
					msgChan $chan Servers on $args have been restarted
				} else {
					msg $chan Invalid server specified. Servers: $servers
				}
			}
		}
	} else {
		msgChan $chan No parameters specified. @restart all, @restart <server>. Servers found using @listservers
	}
}

proc serverbot:pushconfig {nick uhost hand chan args} {
	global regexRB servers
	regsub -all $regexRB $args "" args
	putlog "$nick ($uhost) issued command @pushconfig with args $args"
	msgChan $chan This command does nothing, what were you expecting?
}
	
proc serverbot:newserver {nick uhost hand chan args} {
	global regexRB servers serverarray
	regsub -all $regexRB $args "" args
	putlog "$nick ($uhost) issued command @addserver with args $args"
	serverbot:loadservers
	set server [split $args " "]
	lappend servers [lindex $server 0]
	set serverarray([lindex $server 0]) [lindex $server 1]
	serverbot:saveservers
	msgChan $chan $args added to the server list. Current servers: $servers
}
proc serverbot:delserver {nick uhost hand chan args} {
	global regexRB servers serverarray
	regsub -all $regexRB $args "" args
	putlog "$nick ($uhost) issued command @delserver with args $args"
	serverbot:loadservers
	set servers [lreplace $servers [lsearch $servers $args] [lsearch $servers $args]]
	unset serverarray($args)
	serverbot:saveservers
	msgChan $chan $args deleted from the server list. Current servers: $servers
}
proc serverbot:listservers {nick uhost hand chan args} {
	global regexRB servers serverarray
	regsub -all $regexRB $args "" args
	putlog "$nick ($uhost) issued command @listservers with args $args"
	serverbot:loadservers
	foreach {servername serverip} [array get serverarray] {
		lappend serverstring $serverip ($servername)
	}
	msgChan $chan Servers: $serverstring
}

proc serverbot:loadservers {} {
	global servers regexRB serverarray
	catch {unset servers}
	catch {unset serverarray}
	if {![file exists "servers"]} {
		set serverfile [open "servers" w]
		close $serverfile
	}
	set serverfile [open "servers" r]
	set serverdata [read $serverfile]
	set serverdata [split $serverdata "\n"]
	regsub -all $regexRB $serverdata "" serverdata
	foreach server $serverdata {
		set servername [lindex [split $server "%"] 0]
		set serverip [lindex [split $server "%"] 1]
		lappend servers $servername
		set serverarray($servername) $serverip
	}
	#putlog "Servers loaded: $servers"
	#putlog "[parray serverarray]"
}

proc serverbot:saveservers {} {
	global servers serverarray
	set serverfile [open "servers" w]
	foreach {servername serverip} [array get serverarray] {
		if {($servername != "") && ($serverip != "")} {
			puts $serverfile $servername%$serverip
		}
	}
	close $serverfile
}

proc serverbot:help {nick uhost hand chan args} {
	putserv "NOTICE $nick :Available commands are:"
	putserv "NOTICE $nick : @update <all/server> - Update specified server, or all servers>"
	putserv "NOTICE $nick : @listservers - List servers available"
	putserv "NOTICE $nick : @restart <all/server> - Restart specified server, or all servers"
	putserv "NOTICE $nick : @demosync <all/server> - Sync demos from specified, or all servers"
	#putserv "NOTICE $nick : @pushcfg <config> <location> <new location> - Transfer a config to a server (NYI)"
}

proc serverbot:releasethread {nick uhost hand chan thread} {
	msgChan $chan Releasing thread $thread
	catch { thread::release $thread } released
	msgChan $chan Thread release result: $released
}

proc serverbot:showthreads {nick uhost hand chan text} {
	msgChan $chan Running threads: [thread::names]
}

proc msgChan {chan args} {
	global regexRB adminchan
	regsub -all $regexRB $args "" args
	putlog "iPGN Server Bot: Messaging $chan with $args"
	putquick "PRIVMSG $chan :\00312\253\003 $args \00312\273\003"
	return
}

thread::errorproc serverbot:threaderror

proc serverbot:threaderror {id error} {
	set errorFile [open "errors" w]
	puts $errorFile "Error occured in thread $id"
	puts $errorFile "Error: $error"
	close $errorFile
	msgChan \#ipgn\-tf2 Thread $id died with error: $error
}

serverbot:loadservers

putlog "iPGN Server Interaction script loaded"