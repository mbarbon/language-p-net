#!/usr/bin/perl -w

print "1..2\n";

if( defined &Internals::Net::get_class ) {
    $string_class = Internals::Net::get_class( 'System.String' );
    $char_class = Internals::Net::get_class( 'System.Char' );
    $array_class = Internals::Net::get_class( 'System.Array' );

    $array = $array_class->CreateInstance( $char_class, 10 );
    for( my $i = 0; $i < 10; ++$i ) {
        $array->[$i] = $i + 65;
        $array[$i] = $i + 97;
    }

    $string = Internals::Net::create( $string_class, $array );

    print $string eq 'ABCDEFGHIJ' ? "ok\n" : "not ok - $string\n";

    $string = Internals::Net::create( $string_class, \@array );

    print $string eq 'abcdefghij' ? "ok\n" : "not ok - $string\n";
} else {
    print "ok - skipped\n" for 1..2;
}
