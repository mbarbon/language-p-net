#!/usr/bin/perl -w

print "1..4\n";

if( defined &Internals::Net::get_class ) {
    $class = Internals::Net::get_class( 'System.DateTime' );
    $date = Internals::Net::create( $class, 2010, 12, 16, 22, 13, 42 );

    $kind = Internals::Net::get_property( $date, 'Kind' );
    print $kind == 0 ? "ok\n" : "not ok - $kind\n";
    print $kind eq 'Unspecified' ? "ok\n" : "not ok - $kind\n";

    $utc = Internals::Net::create( $class, 2010, 12, 16, 22, 13, 42, 0, 1 );
    $kind = Internals::Net::get_property( $utc, 'Kind' );
    print $kind == 1 ? "ok\n" : "not ok - $kind\n";
    print $kind eq 'Utc' ? "ok\n" : "not ok - $kind\n";
} else {
    print "ok - skipped\n" for 1..4;
}
