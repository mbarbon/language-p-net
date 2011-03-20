#!/usr/bin/perl -w

print "1..3\n";

if( defined &Internals::Net::get_class ) {
    $class = Internals::Net::get_class( 'System.String' );
    $string = Internals::Net::create( $class, 'a', 20 );

    print $string eq 'aaaaaaaaaaaaaaaaaaaa' ? "ok\n" : "not ok - $string\n";

    $idx = Internals::Net::call_method( $string, 'IndexOf', 'aaaa', 4, 10, 0 );
    print $idx == 4 ? "ok\n" : "not ok - $idx\n";

    $up = Internals::Net::call_method( $string, 'ToUpper' );
    print $up eq 'AAAAAAAAAAAAAAAAAAAA' ? "ok\n" : "not ok - $up\n";
} else {
    print "ok - skipped\n" for 1..3;
}
