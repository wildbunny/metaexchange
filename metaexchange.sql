-- phpMyAdmin SQL Dump
-- version 3.2.4
-- http://www.phpmyadmin.net
--
-- Host: localhost
-- Generation Time: Jan 13, 2015 at 06:55 PM
-- Server version: 5.1.41
-- PHP Version: 5.3.1

SET SQL_MODE="NO_AUTO_VALUE_ON_ZERO";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;

--
-- Database: `metaexchange`
--

--
-- Dumping data for table `exceptions`
--

INSERT INTO `exceptions` (`txid`, `message`, `date`, `bitcoin_deposit`) VALUES
('65c9be36b78f31e40443f5a72c31fc6301fde395', 'Insufficient funds', '2015-01-13 18:51:22', 0);

--
-- Dumping data for table `ignored`
--

INSERT INTO `ignored` (`uid`, `txid`) VALUES
(2, '830277311d0357d6565a78d8d49c5ce3c0945e4cc90f089b07dc21f25d64b8dd'),
(3, '8e8e56c3f05c9e29afd33afd995a479fe56810a69ba042e4af747346206f37ea'),
(4, 'f6aafc07e729e407afd39138090f221c07f9ebc3'),
(5, '82cab23e4168a51e64096e303fe39cb15a31d479'),
(6, 'b522cbdcf48fad1a4d73a54bf0030273ae40871d'),
(7, '9076cce58527d0b92f65b74f9c405c0cb1b733df'),
(8, '65c9be36b78f31e40443f5a72c31fc6301fde395');

--
-- Dumping data for table `stats`
--

INSERT INTO `stats` (`last_bitshares_block`, `last_bitcoin_block`, `uid`) VALUES
(1515896, '000000001462406d0aa91545601758ce4bcbbd0c16251a938cb3c4175f00527a', 1);

--
-- Dumping data for table `transactions`
--

INSERT INTO `transactions` (`bitshares_trx`, `bitcoin_txid`, `asset`, `amount`, `date`, `bitcoin_deposit`) VALUES
('2298af79c55be4e5e1262faa5343616a4064af77', '73abf1c9314ee25c9da206ec74e8af449be6172f3705999d9db33e6e4c231686', 'BTS', '0.40000000', '2015-01-09 16:19:53', 1),
('521e758ae0e3d7a87f8ffd85d3b890bcbc91d48f', 'c7d5af408561dc6fd7f112dcb0d824933caebfeeb3cd8750196c2d9836a79057', 'BTS', '0.00100000', '2015-01-09 15:26:13', 1),
('5262722234a17bdc92d9794a1b7519f300f0b92f', '016d185f4794ad8aa156eb38dd1dba2f95bc6ba7585f1656c6f7304174bf3c88', 'BTS', '0.50000000', '2014-12-23 18:48:19', 0),
('d8ac4891d6f7ccb0a4304afa9c98b533bee4947e', 'a8fa49df7ac59b270e0938bb817e9e01d65eba2218bf324d3ceab18684a5fa70', 'BTS', '0.10000000', '2015-01-09 16:49:53', 0),
('f6aafc07e729e407afd39138090f221c07f9ebc3', '830277311d0357d6565a78d8d49c5ce3c0945e4cc90f089b07dc21f25d64b8dd', 'BTS', '0.01000000', '2014-12-23 18:48:51', 1);

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
